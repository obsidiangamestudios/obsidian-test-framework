using Microsoft.EntityFrameworkCore;

namespace Obsidian.Test.Framework.Tests;

[Parallelizable(ParallelScope.All)]
[DatabaseTestSuite(typeof(GlobalDatabaseSetupFixture))]
public partial class TestSuiteTwo : IAsyncDisposable
{
    private TestDbContext _context = null!;

    protected async partial Task OnSetupAsync()
    {
        _context = GetDbContext();
        await _context.Database.EnsureCreatedAsync();
    }

    protected async partial Task OnTearDownAsync()
    {
        await _context.DisposeAsync();
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

    protected async partial Task OnDisposeAsync()
    {
        await _context.DisposeAsync();
    }


}


