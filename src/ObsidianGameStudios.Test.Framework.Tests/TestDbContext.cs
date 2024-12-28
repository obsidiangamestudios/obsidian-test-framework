using Microsoft.EntityFrameworkCore;

namespace ObsidianGameStudios.Test.Framework.Tests;

public class Post
{
    public int Id { get; set; }
    public string Name { get; set; }
}

public class TestDbContext(DbContextOptions<TestDbContext> options): DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Post>().ToTable("Posts");
        modelBuilder.Entity<Post>().HasKey(p => p.Id);
        modelBuilder.Entity<Post>().Property(p => p.Name).HasMaxLength(100);
    }

    public DbSet<Post> Posts => Set<Post>();
}