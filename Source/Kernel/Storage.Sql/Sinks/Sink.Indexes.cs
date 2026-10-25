// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Arc.Connections;
using Cratis.Arc.EntityFrameworkCore;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;
using Cratis.Chronicle.Storage.Sql.Schema;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.Sinks;

public partial class Sink
{
    static readonly ConcurrentDictionary<string, SemaphoreSlim> _indexGates = new();
    readonly object _indexGate = new();
    readonly HashSet<string> _indexedTables = [];
    readonly Dictionary<string, Task> _indexBuilds = [];

    /// <summary>
    /// Gets completion of the currently scheduled index builds.
    /// </summary>
    internal Task IndexBuildCompletion
    {
        get
        {
            lock (_indexGate) return Task.WhenAll(_indexBuilds.Values.ToArray());
        }
    }

    /// <inheritdoc/>
    public Task EnsureIndexes() => EnsureIndexes(reconcile: false);

    /// <inheritdoc/>
    public Task ReconcileIndexes() => EnsureIndexes(reconcile: true);

    async Task EnsureIndexes(bool reconcile)
    {
        if (_indexes.Count == 0 && !reconcile) return;
        var table = ActiveTableName;
        lock (_indexGate)
        {
            if (_indexedTables.Contains(table)) return;
            if (_indexBuilds.TryGetValue(table, out var running) && !running.IsCompleted) return;
        }
        await using var scope = await _database.ReadModelTable(_eventStoreName, _namespace, table, _columns, ConnectionPurpose.Schema);
        var context = scope.DbContext;
        await context.EnsureTableExists();
        var online = context.Database.GetDatabaseType() != DatabaseType.Sqlite &&
            table == _tableName && await context.Entries.AnyAsync();
        if (!online || DynamicSchemaGuard.PolicyFor(context) == DynamicSchemaPolicy.Refuse)
        {
            await BuildIndexes(table, reconcile, online);
            return;
        }
        lock (_indexGate)
        {
            if (_indexedTables.Contains(table)) return;
            if (_indexBuilds.TryGetValue(table, out var running) && !running.IsCompleted) return;

            // A populated primary table must stay available to the projection and its queries.
            // The worker owns its scope and never retains this call's scoped connection.
            _indexBuilds[table] = Task.Run(() => BuildIndexesInBackground(table, reconcile));
        }
    }

    async Task BuildIndexesInBackground(string table, bool reconcile)
    {
        try
        {
            await BuildIndexes(table, reconcile, online: true);
        }
        catch (Exception exception) when (exception is not OperationCanceledException and not DynamicSchemaObjectNotProvisioned)
        {
            _logger.IndexCreationFailed(exception, table, "background index connection could not be opened");
        }
    }

    async Task BuildIndexes(string table, bool reconcile, bool online)
    {
        await using var scope = await _database.ReadModelTable(_eventStoreName, _namespace, table, _columns, ConnectionPurpose.Schema);
        var context = scope.DbContext;
        var database = context.Database;
        var connection = database.GetDbConnection();
        var gate = _indexGates.GetOrAdd($"{connection.ConnectionString}:{table}", _ => new SemaphoreSlim(1, 1));
        if (reconcile) await gate.WaitAsync();
        else if (!await gate.WaitAsync(0)) return;
        try
        {
            await database.OpenConnectionAsync();
            var schemaLock = DatabaseSchemaLock.For(connection);
            var resource = $"chronicle:read-model-indexes:{table}";
            if (schemaLock is not null) await schemaLock.Acquire(connection, resource, TimeSpan.Zero);
            try
            {
                var complete = await ApplyIndexes(context, table, online);
                if (reconcile && complete) await ReadModelIndexes.DropUndeclared(context, table, IndexPlans(context.Database.GetDatabaseType()).Select(plan => plan.Name(context.Database.GetDatabaseType(), table)));

                // A replay table name is reused after promotion, so its completion cannot be cached.
                if (complete && table == _tableName)
                {
                    lock (_indexGate) _indexedTables.Add(table);
                }
            }
            finally
            {
                if (schemaLock is not null) await schemaLock.Release(connection, resource);
            }
        }
        catch (DatabaseSchemaLockNotAcquired)
        {
            // Another kernel is already building this table's indexes. A later ensure can retry.
        }
        catch (Exception exception) when (exception is not OperationCanceledException and not DynamicSchemaObjectNotProvisioned)
        {
            _logger.IndexCreationFailed(exception, table, "index reconciliation");
        }
        finally
        {
            gate.Release();
        }
    }

    async Task<bool> ApplyIndexes(ReadModelDbContext context, string table, bool online)
    {
        var complete = true;
        foreach (var plan in IndexPlans(context.Database.GetDatabaseType()))
        {
            var path = plan.Definition.PropertyPath;
            try
            {
                var rejection = await SqlServerIndexKeyBudget.RejectionReason(context, table, plan);
                if (rejection is not null)
                {
                    _logger.IndexNotCreated(_tableName, path.Path, rejection);
                    continue;
                }
                await ProjectedColumnIndexes.Ensure(context, table, plan, online);
            }
            catch (Exception exception) when (exception is not OperationCanceledException and not DynamicSchemaObjectNotProvisioned)
            {
                complete = false;
                _logger.IndexCreationFailed(exception, table, path.Path);
            }
        }
        return complete;
    }

    IEnumerable<ReadModelIndexPlan> IndexPlans(DatabaseType type)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var index in _indexes.Distinct().OrderBy(index => index.Signature, StringComparer.Ordinal))
        {
            var plan = ReadModelIndexPlan.Resolve(index, _schema, _columns, type);
            if (plan is null)
            {
                var reason = type == DatabaseType.SqlServer
                    ? "array paths require SQL Server 2025 JSON indexes; other paths must resolve to scalar leaves, and composites cannot contain arrays"
                    : "the path must resolve to a scalar leaf or a standalone PostgreSQL JSON array; composite array indexes are not supported";
                _logger.IndexNotCreated(_tableName, index.PropertyPath.Path, reason);
                continue;
            }
            if (plan.IsGin && plan.Targets.Any(target => target.Descending))
            {
                _logger.ArrayIndexDirectionIgnored(_tableName, index.PropertyPath.Path);
            }
            if (seen.Add(plan.Signature)) yield return plan;
        }
    }
}
