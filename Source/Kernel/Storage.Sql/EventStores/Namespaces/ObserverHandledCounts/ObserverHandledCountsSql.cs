// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ObserverHandledCounts;

/// <summary>
/// Executes bounded additive count statements in one non-retrying transaction.
/// </summary>
internal static class ObserverHandledCountsSql
{
    const int CellsPerStatement = 180;

    /// <summary>
    /// Adds the supplied cells atomically within this call.
    /// </summary>
    /// <param name="context">The namespace context.</param>
    /// <param name="cells">Coalesced count deltas.</param>
    /// <returns>The asynchronous operation.</returns>
    internal static async Task Increment(NamespaceDbContext context, IReadOnlyCollection<ObserverHandledCountEntry> cells)
    {
        var entity = context.Model.FindEntityType(typeof(ObserverHandledCountEntry))!;
        var schema = entity.GetSchema();
        if (schema is null && !context.Database.IsSqlite())
        {
            schema = await context.Database.SqlQueryRaw<string>(context.Database.IsNpgsql()
                ? "SELECT current_schema() AS \"Value\"" : "SELECT SCHEMA_NAME() AS [Value]").SingleAsync();
        }
        var helper = context.GetService<ISqlGenerationHelper>();
        var table = EscapeFormatIdentifier(helper.DelimitIdentifier(entity.GetTableName()!, schema));
        var store = StoreObjectIdentifier.Table(entity.GetTableName()!, entity.GetSchema());
        string Column(string name) => EscapeFormatIdentifier(helper.DelimitIdentifier(entity.FindProperty(name)!.GetColumnName(store)!));
        string[] columns = [Column(nameof(ObserverHandledCountEntry.Id)), Column(nameof(ObserverHandledCountEntry.ObserverId)),
            Column(nameof(ObserverHandledCountEntry.Partition)), Column(nameof(ObserverHandledCountEntry.EventTypeId)), Column(nameof(ObserverHandledCountEntry.Count))];

        // Raw SQL deliberately bypasses automatic execution-strategy replay of additive writes.
        // An unknown commit outcome still requires caller reconciliation, not a blind retry.
        await using var transaction = await context.Database.BeginTransactionAsync();
        foreach (var batch in cells.Chunk(CellsPerStatement))
        {
            var values = batch.SelectMany(row => new object[] { row.Id, row.ObserverId, row.Partition, row.EventTypeId, row.Count }).ToArray();
            var tuples = string.Join(", ", batch.Select((_, index) => $"({string.Join(", ", Enumerable.Range(index * columns.Length, columns.Length).Select(parameter => $"{{{parameter}}}"))})"));
            var sql = context.Database.IsSqlServer()
                ? Merge(table, columns, tuples)
                : Upsert(table, columns, tuples);
            await context.Database.ExecuteSqlInterpolatedAsync(FormattableStringFactory.Create(sql, values));
        }
        await transaction.CommitAsync();
    }

    static string EscapeFormatIdentifier(string identifier)
    {
        // SQL identifier quoting does not escape the separate composite-format grammar.
        return identifier.Replace("{", "{{", StringComparison.Ordinal).Replace("}", "}}", StringComparison.Ordinal);
    }

    static string Upsert(string table, string[] columns, string tuples)
    {
        var keys = string.Join(", ", columns.Skip(1).Take(3));
        var count = columns[4];
        return $"INSERT INTO {table} AS target ({string.Join(", ", columns)}) VALUES {tuples} ON CONFLICT ({keys}) DO UPDATE SET {count} = target.{count} + excluded.{count}";
    }

    static string Merge(string table, string[] columns, string tuples)
    {
        var keys = string.Join(", ", columns.Skip(1).Take(3));
        var count = columns[4];
        var predicate = string.Join(" AND ", columns.Skip(1).Take(3).Select(column => $"target.{column} = source.{column}"));

        // VALUES parameters inherit the database collation, matching the current key columns.
        // Group in SQL as well as in memory: ordinal-distinct keys can name one target row.
        return $"MERGE {table} WITH (HOLDLOCK) AS target USING (SELECT {keys}, SUM({count}) AS {count} FROM (VALUES {tuples}) AS cells ({string.Join(", ", columns)}) GROUP BY {keys}) AS source ON {predicate} " +
            $"WHEN MATCHED THEN UPDATE SET {count} = target.{count} + source.{count} " +
            $"WHEN NOT MATCHED THEN INSERT ({string.Join(", ", columns)}) VALUES (NEWID(), {string.Join(", ", columns.Skip(1).Select(column => $"source.{column}"))});";
    }
}
