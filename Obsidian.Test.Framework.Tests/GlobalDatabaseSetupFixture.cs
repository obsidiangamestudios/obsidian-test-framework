[assembly: LevelOfParallelism(1)]

namespace Obsidian.Test.Framework.Tests;

[DatabaseFixtureGenerator("SQL_SERVER_DB_CONNECTION_STRING", DatabaseProvider.SqlServer, "ObsidianTests", 10)]
public partial class GlobalDatabaseSetupFixture
{

}