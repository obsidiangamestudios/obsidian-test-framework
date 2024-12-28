namespace ObsidianGameStudios.NUnit.Database;

[AttributeUsage(AttributeTargets.Class)]
public class DatabaseTestSuiteAttribute : Attribute
{
    public Type SetupFixtureType { get; }


    public DatabaseTestSuiteAttribute(Type setupFixtureType)
    {
        SetupFixtureType = setupFixtureType;
    }
}