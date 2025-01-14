using Microsoft.EntityFrameworkCore;
using ObsidianGameStudios.NUnit.Database;

namespace ObsidianGameStudios.Test.Framework.Tests;

public abstract class TestSuiteBaseClass
{
    protected DbInfo CurrentDbInfo { get; set; }


    protected abstract Task OnDisposeAsync();

    protected abstract Task OnSetupAsync();

    protected abstract Task OnTearDownAsync();

    protected bool ShouldResetDatabase => true;
}

[DatabaseTestSuite(typeof(GlobalDatabaseSetupFixture))]
[Parallelizable(ParallelScope.None)]
public partial class TestSuiteInheritance : TestSuiteBaseClass, IAsyncDisposable
{
    private TestDbContext _context = null!;


  

    protected override async  Task OnSetupAsync()
    {
        _context = GetDbContext();
        await _context.Database.EnsureCreatedAsync();
    }

    protected override async Task OnTearDownAsync()
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

    protected override async Task OnDisposeAsync()
    {
        await _context.DisposeAsync();
    }

}


