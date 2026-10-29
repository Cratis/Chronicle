// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Arc.EntityFrameworkCore;
using Cratis.Chronicle.Storage.Sql.Sinks;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;

/// <summary>
/// Owns the initialized-marker index independently of declared indexes and replay table names.
/// </summary>
internal static class InitializedMarkerIndex
{
    /// <summary>
    /// The prefix outside declared-index and shadow-index reconciliation.
    /// </summary>
    internal const string Prefix = "chronicle_imx_";

    /// <summary>
    /// Observes entry to the table's marker-index lock in the current asynchronous flow, including SQLite's absent lock.
    /// </summary>
    internal static readonly AsyncLocal<Action?> OnLock = new();

    static readonly ConcurrentDictionary<string, SemaphoreSlim> _gates = new();

    /// <summary>
    /// Checks whether the marker index is missing or has a failed build to drop, without taking any lock.
    /// </summary>
    /// <param name="context">The read-model context.</param>
    /// <param name="cancellationToken">The cancellation token for reading existing indexes.</param>
    /// <returns>Whether <see cref="Ensure"/> would issue DDL.</returns>
    /// <remarks>
    /// Reads the catalog over the context's own connection, so an already-current table takes no lock and opens no
    /// extra connection. <see cref="Ensure"/> reads the catalog again under the table's lock.
    /// </remarks>
    internal static async Task<bool> NeedsIndex(ReadModelDbContext context, CancellationToken cancellationToken = default)
    {
        if (!context.Columns.Any(column => column.Name == WellKnownProperties.ReadModelInstanceInitialized)) return false;
        var indexes = await Existing(context).ToArrayAsync(cancellationToken);
        return !indexes.Any(index => index.IsValid) || indexes.Any(index => !index.IsValid);
    }

    /// <summary>
    /// Ensures an index on the initialized marker under a per-table gate, a per-table database lock and a DDL deadline.
    /// </summary>
    /// <param name="context">The read-model context.</param>
    /// <param name="execution">The online DDL settings, timeout and caller token.</param>
    /// <param name="timeProvider">The clock used for the reconciliation deadline.</param>
    /// <returns>Whether the index is in place; false when another kernel held the table's marker lock past the deadline.</returns>
    /// <remarks>
    /// The lock covers only this table, never the database-wide schema lock, so a long build on one large table
    /// does not hold up schema changes on other tables.
    /// </remarks>
    internal static async Task<bool> Ensure(ReadModelDbContext context, ReadModelIndexOperations.Execution execution, TimeProvider timeProvider)
    {
        if (!context.Columns.Any(column => column.Name == WellKnownProperties.ReadModelInstanceInitialized)) return true;

        var timeout = execution.CommandTimeout.GetValueOrDefault();
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(timeout);
        using var timeoutSource = new CancellationTokenSource(TimeSpan.FromSeconds(timeout), timeProvider);
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(execution.CancellationToken, timeoutSource.Token);
        var callerToken = execution.CancellationToken;
        execution = execution with { CancellationToken = deadline.Token };
        var key = $"{context.Database.GetConnectionString()}:{context.TableName}";
        var gate = _gates.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(deadline.Token);
        try
        {
            return await EnsureUnderTableLock(context, execution, (TimeSpan.FromSeconds(timeout), callerToken));
        }
        finally
        {
            gate.Release();
        }
    }

    static async Task<bool> EnsureUnderTableLock(ReadModelDbContext context, ReadModelIndexOperations.Execution execution, (TimeSpan Wait, CancellationToken CallerToken) acquisition)
    {
        var (wait, callerToken) = acquisition;
        OnLock.Value?.Invoke();
        await context.Database.OpenConnectionAsync(execution.CancellationToken);
        try
        {
            var connection = context.Database.GetDbConnection();
            var schemaLock = DatabaseSchemaLock.For(connection);
            var resource = $"chronicle:read-model-marker-index:{context.TableName}";
            try
            {
                if (schemaLock is not null) await schemaLock.Acquire(connection, resource, wait, execution.CancellationToken);
            }
            catch (Exception exception) when (exception is DatabaseSchemaLockNotAcquired || (exception is OperationCanceledException && !callerToken.IsCancellationRequested))
            {
                // The wait and the DDL deadline share one budget; either expiring while another kernel holds the lock defers the build.
                execution.Logger?.InitializedMarkerIndexDeferred(context.TableName);
                return false;
            }
            try
            {
                await EnsureIndex(context, execution);
                return true;
            }
            finally
            {
                if (schemaLock is not null) await schemaLock.Release(connection, resource);
            }
        }
        finally
        {
            await context.Database.CloseConnectionAsync();
        }
    }

    static async Task EnsureIndex(ReadModelDbContext context, ReadModelIndexOperations.Execution execution)
    {
        var indexes = await Existing(context).ToArrayAsync(execution.CancellationToken);
        foreach (var index in indexes.Where(index => !index.IsValid))
        {
            await ReadModelIndexOperations.Drop(context, (context.TableName, index.Name), execution);
        }
        if (indexes.Any(index => index.IsValid)) return;

        // Keep the name through replay renames; a unique suffix avoids a name owned by a revert table.
        var name = ReadModelIndexNames.For(context.Database.GetDatabaseType(), context.TableName, $"{WellKnownProperties.ReadModelInstanceInitialized}_{Guid.NewGuid():N}")
            .Replace(ReadModelIndexNames.Prefix, Prefix, StringComparison.Ordinal);
        await ReadModelIndexOperations.Ensure(context, (context.TableName, WellKnownProperties.ReadModelInstanceInitialized, name), execution);
    }

    static IQueryable<StoredIndex> Existing(ReadModelDbContext context)
    {
        var table = PrimaryKeyNames.TableIdentifier(context.Database.GetDatabaseType(), context.TableName);
        const string column = WellKnownProperties.ReadModelInstanceInitialized;
        return context.Database.GetDatabaseType() switch
        {
            DatabaseType.PostgreSql => context.Database.SqlQuery<StoredIndex>($"SELECT i.relname AS \"Name\", x.indisvalid AS \"IsValid\" FROM pg_index x JOIN pg_class t ON t.oid = x.indrelid JOIN pg_namespace n ON n.oid = t.relnamespace JOIN pg_class i ON i.oid = x.indexrelid JOIN pg_attribute a ON a.attrelid = t.oid AND a.attnum = x.indkey[0] WHERE t.relname = {table} AND n.nspname = current_schema() AND x.indnatts = 1 AND NOT x.indisunique AND a.attname = {column} AND left(i.relname, 14) = {Prefix}"),
            DatabaseType.SqlServer => context.Database.SqlQuery<StoredIndex>($"SELECT i.name AS [Name], CAST(1 AS bit) AS [IsValid] FROM sys.indexes i JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id WHERE i.object_id = OBJECT_ID(QUOTENAME(SCHEMA_NAME()) + N'.' + QUOTENAME({table})) AND ic.key_ordinal = 1 AND i.is_unique = 0 AND c.name = {column} AND LEFT(i.name, 14) = {Prefix}"),
            _ => context.Database.SqlQuery<StoredIndex>($"SELECT i.name AS Name, 1 AS IsValid FROM pragma_index_list({table}) i JOIN pragma_index_info(i.name) c WHERE i.\"unique\" = 0 AND c.name = {column} AND substr(i.name, 1, 14) = {Prefix}")
        };
    }

    sealed class StoredIndex
    {
        public string Name { get; set; } = string.Empty;
        public bool IsValid { get; set; }
    }
}
