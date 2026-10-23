// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Arc.EntityFrameworkCore;
using Cratis.Chronicle.Storage.Sql.Sinks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;

/// <summary>
/// Owns shadow indexes independently of declared read-model indexes and replay table names.
/// </summary>
internal static class ShadowColumnIndexes
{
    /// <summary>
    /// The prefix ignored by both old and new declared-index reconciliation.
    /// </summary>
    internal const string Prefix = "chronicle_shx_";

    static readonly ConcurrentDictionary<string, SemaphoreSlim> _gates = new();

    /// <summary>
    /// Ensures the registry's shadow indexes on the physical table and removes obsolete shadow indexes.
    /// </summary>
    /// <param name="context">The read-model context.</param>
    /// <param name="configuration">The configured SQL options, or defaults.</param>
    /// <param name="logger">The migration logger for visible DDL failures.</param>
    /// <param name="cancellationToken">An available caller token, linked with the configured reconciliation deadline.</param>
    /// <returns>An awaitable task.</returns>
    internal static async Task Ensure(ReadModelDbContext context, Configuration.Sql? configuration = null, ILogger<ReadModelMigrator>? logger = null, CancellationToken cancellationToken = default)
    {
        configuration ??= new();
        var timeout = configuration.ShadowIndexDdlTimeoutSeconds;
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(timeout);
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(TimeSpan.FromSeconds(timeout));
        var execution = new ReadModelIndexOperations.Execution(Online: true, CommandTimeout: timeout, Logger: logger, CancellationToken: deadline.Token);
        var key = $"{context.Database.GetConnectionString()}:{context.TableName}";
        var gate = _gates.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(deadline.Token);
        try
        {
            await EnsureIndexes(context, execution);
        }
        finally
        {
            gate.Release();
        }
    }

    /// <summary>
    /// Removes the superseded declared non-unique decimal TEXT index, but never a custom or unique index.
    /// </summary>
    /// <param name="context">The SQLite read-model context.</param>
    /// <param name="column">The original decimal property.</param>
    /// <returns>An awaitable task.</returns>
    internal static async Task RemoveLegacyDecimalIndex(ReadModelDbContext context, string column)
    {
        var name = ReadModelIndexNames.For(DatabaseType.Sqlite, context.TableName, column);
        var exists = await context.Database.SqlQuery<string>($"SELECT name AS Value FROM pragma_index_list({context.TableName}) WHERE \"unique\" = 0 AND name = {name}").AnyAsync();
        if (exists) await ReadModelIndexOperations.Drop(context, context.TableName, name, online: true);
    }

    static async Task EnsureIndexes(ReadModelDbContext context, ReadModelIndexOperations.Execution execution)
    {
        var expected = ShadowColumns.IndexedFor(context).ToHashSet(StringComparer.Ordinal);
        if (ShadowColumns.For(context).Count == 0) return;
        var indexes = await Existing(context).ToArrayAsync(execution.CancellationToken);
        foreach (var index in indexes.Where(index => !index.IsValid || !expected.Contains(index.Column)))
        {
            await ReadModelIndexOperations.Drop(context, (context.TableName, index.Name), execution);
        }
        foreach (var column in expected.Where(column => !indexes.Any(index => index.IsValid && index.Column == column)))
        {
            // Names must survive renames untouched. A unique suffix avoids a name still owned by a revert table.
            var name = ReadModelIndexNames.For(context.Database.GetDatabaseType(), context.TableName, $"{column}_{Guid.NewGuid():N}")
                .Replace(ReadModelIndexNames.Prefix, Prefix, StringComparison.Ordinal);
            await ReadModelIndexOperations.Ensure(context, (context.TableName, column, name), execution);
        }
    }

    static IQueryable<StoredIndex> Existing(ReadModelDbContext context)
    {
        var table = PrimaryKeyNames.TableIdentifier(context.Database.GetDatabaseType(), context.TableName);
        return context.Database.GetDatabaseType() switch
        {
            DatabaseType.PostgreSql => context.Database.SqlQuery<StoredIndex>($"SELECT i.relname AS \"Name\", a.attname AS \"Column\", x.indisvalid AS \"IsValid\" FROM pg_index x JOIN pg_class t ON t.oid = x.indrelid JOIN pg_namespace n ON n.oid = t.relnamespace JOIN pg_class i ON i.oid = x.indexrelid JOIN pg_attribute a ON a.attrelid = t.oid AND a.attnum = x.indkey[0] WHERE t.relname = {table} AND n.nspname = current_schema() AND x.indnatts = 1 AND NOT x.indisunique AND left(i.relname, 14) = {Prefix}"),
            DatabaseType.SqlServer => context.Database.SqlQuery<StoredIndex>($"SELECT i.name AS [Name], c.name AS [Column], CAST(1 AS bit) AS [IsValid] FROM sys.indexes i JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id WHERE i.object_id = OBJECT_ID(QUOTENAME(SCHEMA_NAME()) + N'.' + QUOTENAME({table})) AND ic.key_ordinal = 1 AND i.is_unique = 0 AND LEFT(i.name, 14) = {Prefix}"),
            _ => context.Database.SqlQuery<StoredIndex>($"SELECT i.name AS Name, c.name AS \"Column\", 1 AS IsValid FROM pragma_index_list({table}) i JOIN pragma_index_info(i.name) c WHERE i.\"unique\" = 0 AND substr(i.name, 1, 14) = {Prefix}")
        };
    }

    sealed class StoredIndex
    {
        public string Name { get; set; } = string.Empty;
        public string Column { get; set; } = string.Empty;
        public bool IsValid { get; set; }
    }
}
