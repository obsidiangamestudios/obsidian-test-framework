namespace Obsidian.Test.Framework;

[AttributeUsage(AttributeTargets.Class)]
public class DatabaseFixtureGeneratorAttribute : Attribute
{
    public string ConnectionStringName { get; }
    public DatabaseProvider Provider { get; }
    public string ProjectPrefix { get; }
    public int PoolSize { get; }

    public DatabaseFixtureGeneratorAttribute(string connectionStringName, DatabaseProvider provider, string projectPrefix, int poolSize)
    {
        ConnectionStringName = connectionStringName;
        Provider = provider;
        ProjectPrefix = projectPrefix;
        PoolSize = poolSize;
    }
}