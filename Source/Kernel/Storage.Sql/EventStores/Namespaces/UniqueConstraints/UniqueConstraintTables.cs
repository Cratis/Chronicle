// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Chronicle.Concepts.EventSequences;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.UniqueConstraints;

/// <summary>
/// Lists the unique-index tables of one event sequence in a namespace database.
/// </summary>
/// <remarks>
/// Constraint and scope names make the tables dynamic, so finding every claim of a subject means reading the catalog.
/// </remarks>
public static class UniqueConstraintTables
{
    /// <summary>
    /// The suffix of a per-event-source claim table.
    /// </summary>
    public const string PerEventSourceSuffix = "_constraint";

    /// <summary>
    /// The suffix of a per-value claim table.
    /// </summary>
    public const string PerValueSuffix = "_constraint_values";

    /// <summary>
    /// PostgreSQL truncates identifiers to this many bytes (NAMEDATALEN - 1).
    /// </summary>
    const int PostgreSqlMaxIdentifierBytes = 63;

    /// <summary>
    /// Truncation preserves UTF-8 characters, so a four-byte character can leave three unused bytes.
    /// </summary>
    const int PostgreSqlMinTruncatedIdentifierBytes = PostgreSqlMaxIdentifierBytes - 3;

    /// <summary>
    /// Lists the claim tables of the event sequence.
    /// </summary>
    /// <param name="context">Any context connected to the namespace database.</param>
    /// <param name="eventSequenceId">The event sequence.</param>
    /// <param name="cancellationToken">Cancellation of the read.</param>
    /// <returns>The per-event-source and per-value table names.</returns>
    public static async Task<(string[] PerEventSource, string[] PerValue)> For(DbContext context, EventSequenceId eventSequenceId, CancellationToken cancellationToken = default)
    {
        var names = await TableNames(context).ToArrayAsync(cancellationToken);
        var primaryKeys = context.Database.IsNpgsql()
            ? await PrimaryKeyColumns(context).ToArrayAsync(cancellationToken)
            : null;
        return Classify(names, eventSequenceId, primaryKeys);
    }

    /// <summary>
    /// Classifies catalog table names into claim tables of the event sequence.
    /// </summary>
    /// <param name="names">All table names in the catalog.</param>
    /// <param name="eventSequenceId">The event sequence.</param>
    /// <param name="primaryKeys">Entries of <c>table|primary key column</c> on PostgreSQL, or null for other providers.</param>
    /// <returns>The per-event-source and per-value table names.</returns>
    /// <remarks>
    /// A name is classified by its suffix, except on PostgreSQL near its byte limit, where truncation may remove or
    /// change the suffix. There the primary key identifies the table: <c>EventSourceId</c> for per-event-source claims,
    /// <c>Value</c> for per-value claims. This also handles names that fit the limit exactly.
    /// </remarks>
    internal static (string[] PerEventSource, string[] PerValue) Classify(IEnumerable<string> names, EventSequenceId eventSequenceId, IEnumerable<string>? primaryKeys)
    {
        var prefix = $"{eventSequenceId.Value}_";
        var candidates = names.Where(name => name.StartsWith(prefix, StringComparison.Ordinal)).ToArray();
        var keyOf = (primaryKeys ?? [])
            .Where(_ => _.Contains('|'))
            .ToLookup(_ => _[.._.LastIndexOf('|')], _ => _[(_.LastIndexOf('|') + 1)..]);

        bool IsClaimTable(string name, string suffix, string keyColumn) =>
            primaryKeys is not null && Encoding.UTF8.GetByteCount(name) >= PostgreSqlMinTruncatedIdentifierBytes
                ? keyOf[name].SequenceEqual([keyColumn])
                : name.EndsWith(suffix, StringComparison.Ordinal);

        return (
            candidates.Where(name => IsClaimTable(name, PerEventSourceSuffix, nameof(UniqueConstraintIndexEntry.EventSourceId))).ToArray(),
            candidates.Where(name => IsClaimTable(name, PerValueSuffix, nameof(UniqueConstraintValueEntry.Value))).ToArray());
    }

    static IQueryable<string> PrimaryKeyColumns(DbContext context) =>
        context.Database.SqlQuery<string>($"SELECT kcu.table_name || '|' || kcu.column_name AS \"Value\" FROM information_schema.table_constraints tc JOIN information_schema.key_column_usage kcu ON kcu.constraint_name = tc.constraint_name AND kcu.constraint_schema = tc.constraint_schema AND kcu.table_name = tc.table_name AND kcu.table_schema = tc.table_schema WHERE tc.constraint_type = 'PRIMARY KEY' AND tc.table_schema = current_schema()");

    static IQueryable<string> TableNames(DbContext context)
    {
        if (context.Database.IsSqlServer())
        {
            return context.Database.SqlQuery<string>($"SELECT TABLE_NAME AS [Value] FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = SCHEMA_NAME()");
        }

        if (context.Database.IsNpgsql())
        {
            return context.Database.SqlQuery<string>($"SELECT table_name AS \"Value\" FROM information_schema.tables WHERE table_schema = current_schema()");
        }

        return context.Database.SqlQuery<string>($"SELECT name AS \"Value\" FROM sqlite_master WHERE type = 'table'");
    }
}
