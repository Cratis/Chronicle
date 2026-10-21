// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Arc.Connections;
using Cratis.Arc.EntityFrameworkCore;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;
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
        if (!online)
        {
            await BuildIndexes(table, reconcile, online: false);
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
        catch (Exception exception) when (exception is not OperationCanceledException)
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
                if (reconcile && complete) await ReadModelIndexes.DropUndeclared(context, table, _indexes.Select(index => index.PropertyPath.Path));

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
        catch (Exception exception) when (exception is not OperationCanceledException)
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
        foreach (var index in _indexes)
        {
            var path = index.PropertyPath;
            var column = path.Segments.Count() == 1 ? _columns.FirstOrDefault(column => column.Name == path.Path) : null;
            if (column is not { IsJson: false, IsArray: false })
            {
                _logger.IndexNotCreated(_tableName, path.Path, "only top-level scalar columns are supported; nested paths, objects and arrays require SQL expression indexes");
                continue;
            }
            if (context.Database.GetDatabaseType() == DatabaseType.SqlServer && column.ClrType == typeof(string))
            {
                var length = await context.Database.SqlQuery<int>($"SELECT CAST(CHARACTER_MAXIMUM_LENGTH AS int) AS [Value] FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA=SCHEMA_NAME() AND TABLE_NAME={table} AND COLUMN_NAME={column.Name}").SingleOrDefaultAsync();
                if (length is < 1 or > 850)
                {
                    _logger.IndexNotCreated(_tableName, path.Path, "the existing string column is unbounded or exceeds the SQL Server nonclustered key limit of 1700 bytes; replay to apply a supported bound");
                    continue;
                }
            }
            try
            {
                await ReadModelIndexes.Ensure(context, table, column.Name, online);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                complete = false;
                _logger.IndexCreationFailed(exception, table, path.Path);
            }
        }
        return complete;
    }
}
