namespace ObsidianGameStudios.NUnit.Database;

[AttributeUsage(AttributeTargets.Class)]
public class DatabaseTestSuiteAttribute(Type setupFixtureType) : Attribute
{
    public Type SetupFixtureType { get; } = setupFixtureType;
}