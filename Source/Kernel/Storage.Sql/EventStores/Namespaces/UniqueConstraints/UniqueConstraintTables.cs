// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
    /// Lists the claim tables of the event sequence.
    /// </summary>
    /// <param name="context">Any context connected to the namespace database.</param>
    /// <param name="eventSequenceId">The event sequence.</param>
    /// <param name="cancellationToken">Cancellation of the read.</param>
    /// <returns>The per-event-source and per-value table names.</returns>
    public static async Task<(string[] PerEventSource, string[] PerValue)> For(DbContext context, EventSequenceId eventSequenceId, CancellationToken cancellationToken = default)
    {
        var prefix = $"{eventSequenceId.Value}_";
        var names = (await TableNames(context).ToArrayAsync(cancellationToken)).Where(name => name.StartsWith(prefix, StringComparison.Ordinal)).ToArray();
        return (
            names.Where(name => name.EndsWith(PerEventSourceSuffix, StringComparison.Ordinal)).ToArray(),
            names.Where(name => name.EndsWith(PerValueSuffix, StringComparison.Ordinal)).ToArray());
    }

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
