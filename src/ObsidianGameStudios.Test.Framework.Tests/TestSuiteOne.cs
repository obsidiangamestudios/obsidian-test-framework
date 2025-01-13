using Microsoft.EntityFrameworkCore;
using ObsidianGameStudios.NUnit.Database;

namespace ObsidianGameStudios.Test.Framework.Tests;

[DatabaseTestSuite(typeof(GlobalDatabaseSetupFixture))]
[Parallelizable(ParallelScope.None)]
public partial class TestSuiteOne
{
    private TestDbContext context;

    protected partial bool ShouldResetDatabase => true;

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
