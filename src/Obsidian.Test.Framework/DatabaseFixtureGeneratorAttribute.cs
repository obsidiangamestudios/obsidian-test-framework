namespace Obsidian.Test.Framework;

[AttributeUsage(AttributeTargets.Class)]
public class DatabaseFixtureGeneratorAttribute : Attribute
{
    public string ConnectionStringName { get; }
    public DatabaseProvider Provider { get; }
    public int PoolSize { get; }

    public bool DropDatabase { get; }

    public DatabaseFixtureGeneratorAttribute(
        string connectionStringName,
        DatabaseProvider provider,
        int poolSize,
        bool dropDatabase = false
    )
    {
        ConnectionStringName = connectionStringName;
        Provider = provider;
        PoolSize = poolSize;
        DropDatabase = dropDatabase;
    }
}
