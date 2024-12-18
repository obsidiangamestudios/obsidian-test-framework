using Microsoft.EntityFrameworkCore;

namespace Obsidian.Test.Framework.Tests;

[Parallelizable(ParallelScope.All)]
public class TestSuiteTwo : IAsyncDisposable
{
    private DbInfo _dbInfo;
    private TestDbContext context;


    [SetUp]
    public async Task InitializeEachRun()
    {
        _dbInfo = GlobalDatabaseSetupFixture.DatabaseFixture.TakeOne();
        context = GetDbContext();
        await context.Database.EnsureCreatedAsync();
        await GlobalDatabaseSetupFixture.DatabaseFixture.ResetDatabaseAsync(_dbInfo);
    }

    [TearDown]
    public async Task ReleaseEachRun()
    {
        GlobalDatabaseSetupFixture.DatabaseFixture.ReturnOne(_dbInfo);
        await context.DisposeAsync();
    }


    private TestDbContext GetDbContext()
    {
        var optionsBuilder = new DbContextOptionsBuilder<TestDbContext>();
        optionsBuilder.UseSqlServer(_dbInfo.ConnectionString);

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

    public async ValueTask DisposeAsync()
    {

        await context.DisposeAsync();
    }

    [Test]
    public async Task DelayedTask()
    {
        await Task.Delay(1000);
    }

}