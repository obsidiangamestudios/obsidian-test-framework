using System.Data.Common;
using Microsoft.Data.SqlClient;

namespace Obsidian.Test.Framework.SqlServer;

public class DatabaseIntegrationFixtureSqlServer(string connectionString, int poolSize, string testProjectPrefix) : DatabaseIntegrationFixture(testProjectPrefix)
{
    private readonly SqlConnectionStringBuilder _builder = new(connectionString);

    public override async Task InitializeAsync()
    {
        await using var mainConnection = new SqlConnection(_builder.ConnectionString);
        await mainConnection.OpenAsync();
        for(var i = 0; i < poolSize; i++)
        {
            var dbName = GetDbName(i);
            var dbInfo = new DbInfo(_builder.ConnectionString.Replace(_builder.InitialCatalog, dbName), dbName, i);
            await using var command = mainConnection.CreateCommand();
            command.CommandText = $"SELECT COUNT(*) FROM sys.databases WHERE name = '{dbName}'";
            Ready.Push(dbInfo);
            All.Add(dbInfo);
            if ((int)await command.ExecuteScalarAsync() > 0)
            {
                continue;
            }

            command.CommandText = $"CREATE DATABASE {dbName}";
            await command.ExecuteNonQueryAsync();
            await using var connection = GetConnection(dbInfo);
            await connection.OpenAsync();
        }
    }

    public override async ValueTask DisposeAsync()
    {
        await using var mainConnection = new SqlConnection(_builder.ConnectionString);
        await mainConnection.OpenAsync();
        foreach (var dbInfo in All)
        {
            await using var closeConnectionsCommand = mainConnection.CreateCommand();
            closeConnectionsCommand.CommandText = $"ALTER DATABASE [{dbInfo.DatabaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE";
            await closeConnectionsCommand.ExecuteNonQueryAsync();

            await using var dropCommand = mainConnection.CreateCommand();
            dropCommand.CommandText = $"DROP DATABASE {dbInfo.DatabaseName}";
            await dropCommand.ExecuteNonQueryAsync();
        }
        Ready.Clear();
        Used.Clear();
        All.Clear();
    }

    public override DbConnection GetConnection(DbInfo dbInfo)
    {
        return new SqlConnection(dbInfo.ConnectionString);
    }
}