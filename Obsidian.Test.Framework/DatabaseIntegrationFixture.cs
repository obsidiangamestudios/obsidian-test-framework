using System.Data.Common;
using System.Data.SqlClient;
using Microsoft.Data.SqlClient;
using Respawn;
using Respawn.Graph;

namespace Obsidian.Test.Framework;

public abstract class DatabaseIntegrationFixture(string testProjectPrefix) : IAsyncDisposable
{
    protected const string DbPrefix = "pooled_{0}_{1}";
    protected string TestProjectPrefix => testProjectPrefix;

    protected readonly List<DbInfo> All = new();
    protected readonly Stack<DbInfo> Ready = new();
    protected readonly Stack<DbInfo> Used = new();

    public Table[] TablesToIgnore { get; init; } = [];
    public string[] SchemasToExclude { get; init; } = [];

    public abstract Task InitializeAsync();

    public abstract ValueTask DisposeAsync();

    protected async Task ResetDatabaseAsync(DbConnection connection)
    {
        await connection.OpenAsync();
        var respawner = await Respawner.CreateAsync(connection, new RespawnerOptions
        {
            TablesToIgnore = TablesToIgnore,
            SchemasToExclude = SchemasToExclude
        });

        await respawner.ResetAsync(connection);
    }

    public abstract DbConnection GetConnection(DbInfo dbInfo);

    public async Task ResetDatabaseAsync(DbInfo dbInfo)
    {
        await using var connection = GetConnection(dbInfo);
        await ResetDatabaseAsync(connection);
    }

    protected string GetDbName(int poolIndex)
    {
        return string.Format(DbPrefix, TestProjectPrefix.ToLowerInvariant(), poolIndex.ToString());
    }

    public DbInfo? TakeOne()
    {
        if (Ready.Count == 0)
        {
            return null;
        }

        var dbInfo = Ready.Pop();
        Used.Push(dbInfo);
        return dbInfo;
    }

    public void ReturnOne(DbInfo dbInfo)
    {
        if (!Used.Contains(dbInfo)) return;
        Used.Pop();
        Ready.Push(dbInfo);
    }
}


public record DbInfo(string ConnectionString, string DatabaseName, int PoolIndex);

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



