using System.Data.Common;
using Microsoft.Data.SqlClient;
using Respawn;

namespace ObsidianGameStudios.NUnit.Database.SqlServer;

public class DatabaseIntegrationFixtureSqlServer(string connectionString, int poolSize, string testProjectPrefix, bool dropDatabase) : DatabaseIntegrationFixture(testProjectPrefix)
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
            Ready.Enqueue(dbInfo);
            All.Add(dbInfo);

            command.CommandText = $"""
                                    IF NOT EXISTS (
                                        SELECT name 
                                        FROM sys.databases 
                                        WHERE name = '{dbName}'
                                    )
                                    BEGIN
                                        CREATE DATABASE [{dbName}];
                                        ALTER DATABASE [{dbName}] SET RECOVERY SIMPLE;
                                    END;
                                    """;
            await command.ExecuteNonQueryAsync();
            await using var connection = GetConnection(dbInfo);
            await connection.OpenAsync();
        }
    }

    public override async ValueTask DisposeAsync()
    {
        if(!dropDatabase)
        {
            Ready.Clear();
            All.Clear();

            return;
        }
        await using var mainConnection = new SqlConnection(_builder.ConnectionString);
        await mainConnection.OpenAsync();

        string dropAllDatabasesCommand = DropAllDatabasesCommand(All.Select(db => db.DatabaseName));
        await using var dropAllDatabases = mainConnection.CreateCommand();
        dropAllDatabases.CommandText = dropAllDatabasesCommand;
        dropAllDatabases.CommandTimeout = 120;
        await dropAllDatabases.ExecuteNonQueryAsync();

        Ready.Clear();
        All.Clear();
    }

    protected override async Task ResetDatabaseAsync(DbConnection connection)
    {
        await base.ResetDatabaseAsync(connection);
        await using var dbCommand = connection.CreateCommand();
        dbCommand.CommandText = $"CHECKPOINT;DBCC CHECKDB ('{connection.Database}');DBCC FREEPROCCACHE;";

        await dbCommand.ExecuteNonQueryAsync();
    }

    private static string DropAllDatabasesCommand(IEnumerable<string> dbNames)
    {
        string names = string.Join(", ", dbNames.Select(n => $"('{n}')"));
        return $"""
                DECLARE @databasesToDrop TABLE (DatabaseName NVARCHAR(255));
                
                -- Add the databases you want to drop
                INSERT INTO @databasesToDrop (DatabaseName)
                VALUES {names};
                
                DECLARE @dbName NVARCHAR(255);
                DECLARE @sql NVARCHAR(MAX);
                
                WHILE EXISTS (SELECT 1 FROM @databasesToDrop)
                BEGIN
                    SELECT TOP 1 @dbName = DatabaseName FROM @databasesToDrop;
                
                    -- Set database to SINGLE_USER mode to terminate connections
                    SET @sql = 'ALTER DATABASE [' + @dbName + '] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;';
                    EXEC sp_executesql @sql;
                
                    -- Drop the database
                    SET @sql = 'DROP DATABASE [' + @dbName + '];';
                    EXEC sp_executesql @sql;
                
                    -- Remove the processed database from the list
                    DELETE FROM @databasesToDrop WHERE DatabaseName = @dbName;
                END

                """;
    }

    public override DbConnection GetConnection(DbInfo dbInfo)
    {
        return new SqlConnection(dbInfo.ConnectionString);
    }

    protected override IDbAdapter DbAdapter { get; } = Respawn.DbAdapter.SqlServer;

}