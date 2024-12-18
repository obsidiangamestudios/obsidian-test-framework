using Obsidian.Test.Framework.SqlSever;

[assembly: LevelOfParallelism(1)]

namespace Obsidian.Test.Framework.Tests;

[SetUpFixture]
public class GlobalDatabaseSetupFixture
{
    public static DatabaseIntegrationFixtureSqlServer DatabaseFixture;

    public GlobalDatabaseSetupFixture()
    {
        string connString = Environment.GetEnvironmentVariable("SQL_SERVER_DB_CONNECTION_STRING") ?? throw new Exception("SQL_SERVER_DB_CONNECTION_STRING environment variable not set.");
        DatabaseFixture = new DatabaseIntegrationFixtureSqlServer(connString, 10, "ObsidianTests");
    }

    [OneTimeSetUp]
    public async Task RunBeforeAnyTests()
    {
        await DatabaseFixture.InitializeAsync();
    }

    [OneTimeTearDown]
    public async Task RunAfterAllTests()
    {
        await DatabaseFixture.DisposeAsync();
    }
}