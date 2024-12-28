using ObsidianGameStudios.NUnit.Database;

[assembly: LevelOfParallelism(1)]

namespace ObsidianGameStudios.Test.Framework.Tests;

[DatabaseFixtureGenerator("SQL_SERVER_DB_CONNECTION_STRING", DatabaseProvider.SqlServer, 10, dropDatabase: false)]
public partial class GlobalDatabaseSetupFixture
{

}