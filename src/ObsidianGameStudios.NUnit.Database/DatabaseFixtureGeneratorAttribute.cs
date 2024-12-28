namespace ObsidianGameStudios.NUnit.Database;

[AttributeUsage(AttributeTargets.Class)]
public class DatabaseFixtureGeneratorAttribute(
    string connectionStringName,
    DatabaseProvider provider,
    int poolSize,
    bool dropDatabase = false)
    : Attribute
{
    public string ConnectionStringName { get; } = connectionStringName;
    public DatabaseProvider Provider { get; } = provider;
    public int PoolSize { get; } = poolSize;
    public bool DropDatabase { get; } = dropDatabase;
}
