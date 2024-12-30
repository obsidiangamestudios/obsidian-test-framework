using ObsidianGameStudios.NUnit.Database;

[assembly: LevelOfParallelism(2)]

namespace ObsidianGameStudios.Test.Framework.Tests;

[DatabaseFixtureGenerator(connectionStringName: "SQL_SERVER_DB_CONNECTION_STRING", provider: DatabaseProvider.SqlServer, poolSize: 2, dropDatabase: false)]
public partial class GlobalDatabaseSetupFixture
{

}