using System.Data.Common;
using Respawn;
using Respawn.Graph;

namespace ObsidianGameStudios.NUnit.Database;

public abstract class DatabaseIntegrationFixture(string testProjectPrefix) : IAsyncDisposable
{
    protected const string DbPrefix = "pooled_{0}_{1}";
    protected string TestProjectPrefix => testProjectPrefix;

    protected readonly List<DbInfo> All = new();
    protected readonly Stack<DbInfo> Ready = new();
    protected readonly Stack<DbInfo> Used = new();

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

    protected string GetDbName(int poolIndex)
    {
        return string.Format(DbPrefix, TestProjectPrefix.ToLowerInvariant(), poolIndex.ToString());
    }

    public virtual DbInfo? TakeOne()
    {
        if (Ready.Count == 0)
        {
            return null;
        }

        var dbInfo = Ready.Pop();
        Used.Push(dbInfo);
        return dbInfo;
    }

    public virtual void ReturnOne(DbInfo dbInfo)
    {
        if (!Used.Contains(dbInfo)) return;
        Used.Pop();
        Ready.Push(dbInfo);
    }
}


public record DbInfo(string ConnectionString, string DatabaseName, int PoolIndex);