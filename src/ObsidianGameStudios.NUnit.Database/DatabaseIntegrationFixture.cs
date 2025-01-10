using System.Data.Common;
using System.Reflection;
using System.Runtime.Versioning;
using System.Text.RegularExpressions;
using Respawn;
using Respawn.Graph;

namespace ObsidianGameStudios.NUnit.Database;

public abstract class DatabaseIntegrationFixture(string testProjectPrefix) : IAsyncDisposable
{
    protected const string DbPrefix = "pooled_{0}_{1}_{2}";
    protected string TestProjectPrefix => testProjectPrefix;

    protected readonly List<DbInfo> All = new();
    protected readonly Queue<DbInfo> Ready = new();

    public Table[] TablesToIgnore { get; init; } = [];
    public string[] SchemasToExclude { get; init; } = [];

    public abstract Task InitializeAsync();

    public abstract ValueTask DisposeAsync();

    protected virtual async Task ResetDatabaseAsync(DbConnection connection)
    {
        await connection.OpenAsync();
        var respawner = await Respawner.CreateAsync(connection, new RespawnerOptions
        {
            TablesToIgnore = TablesToIgnore,
            SchemasToExclude = SchemasToExclude,
            DbAdapter = DbAdapter
        });

        await respawner.ResetAsync(connection);
    }

    protected abstract IDbAdapter DbAdapter { get; }

    public abstract DbConnection GetConnection(DbInfo dbInfo);

    public async Task ResetDatabaseAsync(DbInfo dbInfo)
    {
        await using var connection = GetConnection(dbInfo);
        await ResetDatabaseAsync(connection);
    }

    private static string GetShortTargetFramework()
    {
        var tfa = Assembly
            .GetExecutingAssembly()
            .GetCustomAttribute<TargetFrameworkAttribute>();

        var frameworkName = tfa?.FrameworkName ?? "Unknown";

        // Examples of frameworkName:
        //  • ".NETCoreApp,Version=v6.0" => we want "net6.0"
        //  • ".NETCoreApp,Version=v8.0" => we want "net8.0"
        //  • ".NETCoreApp,Version=v3.1" => we might want "netcoreapp3.1"
        //  • ".NETFramework,Version=v4.7.2" => we might want "net472"

        // 1) Check if it's .NETCoreApp
        var matchCore = Regex.Match(frameworkName, @"^\.NETCoreApp,Version=v(?<version>\d+(\.\d+)*)$");
        if (matchCore.Success)
        {
            var version = matchCore.Groups["version"].Value; // e.g. "8.0", "6.0", "3.1", etc.

            // By convention:
            //  - net5.0, net6.0, net7.0, net8.0, etc.
            //  - but .NET Core 3.1 usually is "netcoreapp3.1" in many csproj files
            if (version.StartsWith("3."))
            {
                return "netcoreapp" + version; // netcoreapp3.1
            }
            else
            {
                return "net" + version; // net6.0, net7.0, net8.0, etc.
            }
        }

        // 2) Check if it's .NETFramework (e.g. ".NETFramework,Version=v4.7.2" => net472)
        var matchFx = Regex.Match(frameworkName, @"^\.NETFramework,Version=v(?<version>\d+(\.\d+)*)$");
        if (matchFx.Success)
        {
            var version = matchFx.Groups["version"].Value; // e.g. "4.7.2"
            // Remove the dot(s) => "472"
            var numeric = version.Replace(".", "");
            return "net" + numeric;
        }

        // 3) Fallback
        return frameworkName;
    }

    protected string GetDbName(int poolIndex)
    {
        return string.Format(DbPrefix, GetShortTargetFramework().Replace(".", ""), TestProjectPrefix.ToLowerInvariant(), poolIndex.ToString());
    }

    public virtual DbInfo? TakeOne()
    {
        if (Ready.Count == 0)
        {
            return null;
        }

        var dbInfo = Ready.Dequeue();
        return dbInfo;
    }

    public virtual void ReturnOne(DbInfo dbInfo)
    {
        Ready.Enqueue(dbInfo);
    }
}


public record DbInfo(string ConnectionString, string DatabaseName, int PoolIndex)
{
    public override string ToString()
    {
        return $"DbInfo: {DatabaseName} - {PoolIndex}";
    }
}