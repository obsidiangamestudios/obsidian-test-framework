using Microsoft.EntityFrameworkCore;

namespace Obsidian.Test.Framework.Tests;

[Parallelizable(ParallelScope.All)]
public class TestSuiteTwo : IAsyncDisposable
{
    private DbInfo _dbInfo;
    private TestDbContext _context;

    [SetUp]
    public async Task InitializeEachRun()
    {
        _dbInfo = GlobalDatabaseSetupFixture.DatabaseFixture.TakeOne();
        _context = GetDbContext();
        await _context.Database.EnsureCreatedAsync();
        await GlobalDatabaseSetupFixture.DatabaseFixture.ResetDatabaseAsync(_dbInfo);
    }

    [TearDown]
    public async Task ReleaseEachRun()
    {
        GlobalDatabaseSetupFixture.DatabaseFixture.ReturnOne(_dbInfo);
        await _context.DisposeAsync();
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
        Assert.That(await _context.Posts.CountAsync(), Is.EqualTo(0));
    }

    [Test]
    public async Task CreateOnePost()
    {
        var post = new Post { Name = "Post 1" };
        _context.Posts.Add(post);
        await _context.SaveChangesAsync();
        Assert.That(await _context.Posts.CountAsync(), Is.EqualTo(1));
    }

    public async ValueTask DisposeAsync()
    {

        await _context.DisposeAsync();
    }

    [Test]
    public async Task DelayedTask()
    {
        await Task.Delay(1000);
    }

}