# Obsidian Game Studios NUnit Extensions

For the license please see the [LICENSE](LICENSE) file, at the root of the repository.

The project currently implements the management of a database pool for integration tests with SqlServer.

The library provides a pool with a predefined number of databases that are created at the start of the test and optionally destroyed at its conclusion.

To simplify the system setup, we recommend using the source generator ObsidianGameStudios.NUnit.Database.SourceGenerators, which allows for the automatic generation of test classes with setup and teardown methods.


## Usage

### Installation
To use the library, you need to install the NuGet package ObsidianGameStudios.NUnit.Database.SqlServer and the corresponding source generator package ObsidianGameStudios.NUnit.Database.SourceGenerators.

### Configuration

Let’s see how to set up the global fixture for database management. 
Note that LevelOfParallelism and poolSize must match. 
This ensures that the number of databases created is sufficient to support the number of tests running in parallel, and limiting the number of parallel tests is useful to reduce the load on the test database server.
Also make sure that the connection string is set on your environment variables, with the name you specified, this is useful both for local and for CI/CD pipelines.

```csharp
[assembly: LevelOfParallelism(2)]

namespace ObsidianGameStudios.Test.Framework.Tests;

[DatabaseFixtureGenerator(connectionStringName: "SQL_SERVER_DB_CONNECTION_STRING", provider: DatabaseProvider.SqlServer, poolSize: 2, dropDatabase: false)]
public partial class GlobalDatabaseSetupFixture
{

}
```

### Test class example

On the following example we have a test class that uses the global fixture defined above, and use EntityFrameworkCore to interact with the database.
You can see that we are using the source generator to generate the setup and teardown methods, and the OnDisposeAsync method is used to dispose the context.
The source generator will generate the partial methods OnSetupAsync, OnTearDownAsync and OnDisposeAsync, and you can implement them as needed.
And on the generate class it will handle the picking of the database from the pool, and returning it to the pool at the end of the test.
Also the system implement thanks to Respawn, a way to clean the database between tests, so you can be sure that the database is clean at the start of each test.
But if you need to clean the database between tests, you can implement the OnTearDownAsync method and use the context to clean the database.

```csharp


[DatabaseTestSuite(typeof(GlobalDatabaseSetupFixture))]
public partial class TestSuiteOne
{
    private TestDbContext context;

    protected partial async Task OnSetupAsync()
    {
        context = GetDbContext();
        await context.Database.EnsureCreatedAsync();
    }

    protected partial async Task OnTearDownAsync()
    { 
        await context.Database.CloseConnectionAsync();
        await context.DisposeAsync();
    }


    private TestDbContext GetDbContext()
    {
        var optionsBuilder = new DbContextOptionsBuilder<TestDbContext>();
        optionsBuilder.UseSqlServer(CurrentDbInfo.ConnectionString);

        return new TestDbContext(optionsBuilder.Options);
    }

    [Test]
    public async Task CheckIfPostCollectionIsEmpty()
    {
        Assert.That(await context.Posts.CountAsync(), Is.EqualTo(0));
    }

    [Test]
    public async Task CreateOnePost()
    {
        var post = new Post { Name = "Post 1" };
        context.Posts.Add(post);
        await context.SaveChangesAsync();
        Assert.That(await context.Posts.CountAsync(), Is.EqualTo(1));
    }


    protected async partial Task OnDisposeAsync()
    {
        await context.DisposeAsync();
    }
}
```