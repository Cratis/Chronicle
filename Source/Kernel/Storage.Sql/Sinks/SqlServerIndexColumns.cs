// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;
using Cratis.Chronicle.Storage.Sql.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

namespace Cratis.Chronicle.Storage.Sql.Sinks;

/// <summary>
/// Maintains non-persisted computed columns used by managed indexes.
/// </summary>
internal static class SqlServerIndexColumns
{
    const string Prefix = "chronicle_cc_";

    /// <summary>
    /// Gets the table-independent computed-column name.
    /// </summary>
    /// <param name="target">The resolved property.</param>
    /// <returns>The computed-column name.</returns>
    internal static string Name(ReadModelIndexTarget target)
    {
        var signature = JsonSerializer.Serialize(new { target.Column.Name, target.JsonSegments, target.Type });
        return Prefix + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(signature)))[..16].ToLowerInvariant();
    }

    /// <summary>
    /// Adds missing non-persisted computed columns.
    /// </summary>
    /// <param name="context">The read-model context.</param>
    /// <param name="table">The table name.</param>
    /// <param name="targets">The resolved properties.</param>
    /// <returns>An awaitable task.</returns>
    internal static async Task Ensure(ReadModelDbContext context, string table, IEnumerable<ReadModelIndexTarget> targets)
    {
        var helper = context.GetService<ISqlGenerationHelper>();
        foreach (var target in targets.Where(target => target.IsExpression).DistinctBy(Name))
        {
            var name = Name(target);
            var exists = await context.Database.SqlQuery<int>($"SELECT 1 AS [Value] FROM sys.computed_columns WHERE object_id=OBJECT_ID(QUOTENAME(SCHEMA_NAME()) + N'.' + QUOTENAME({table})) AND name={name}").AnyAsync();
            if (!exists)
            {
                using var warnings = DynamicSchemaGuard.For(context).FlushOnDispose();
                DynamicSchemaGuard.For(context).Require(context, "column", $"{table}.{name}");

                // NON-persisted: column creation is metadata-only, even on a populated primary.
                await Execute(context, $"ALTER TABLE {helper.DelimitIdentifier(table)} ADD {helper.DelimitIdentifier(name)} AS ({ReadModelIndexPlan.SqlServerExpression(target, helper)})");
            }
        }
    }

    /// <summary>
    /// Drops unused Chronicle-owned non-persisted computed columns.
    /// </summary>
    /// <param name="context">The read-model context.</param>
    /// <param name="table">The table name.</param>
    /// <returns>An awaitable task.</returns>
    internal static async Task DropUnused(ReadModelDbContext context, string table)
    {
        var names = await context.Database.SqlQuery<string>($"SELECT c.name AS [Value] FROM sys.computed_columns c WHERE c.is_persisted=0 AND c.object_id=OBJECT_ID(QUOTENAME(SCHEMA_NAME()) + N'.' + QUOTENAME({table})) AND NOT EXISTS (SELECT 1 FROM sys.index_columns i WHERE i.object_id=c.object_id AND i.column_id=c.column_id)").ToListAsync();
        var helper = context.GetService<ISqlGenerationHelper>();
        foreach (var name in names.Where(name => name.StartsWith(Prefix, StringComparison.Ordinal)))
        {
            using var warnings = DynamicSchemaGuard.For(context).FlushOnDispose();
            DynamicSchemaGuard.For(context).Require(context, "column", $"{table}.{name}");
            await Execute(context, $"ALTER TABLE {helper.DelimitIdentifier(table)} DROP COLUMN {helper.DelimitIdentifier(name)}");
        }
    }

    static Task<int> Execute(ReadModelDbContext context, string sql)
    {
#pragma warning disable EF1002 // Only provider-delimited identifiers and schema-derived expressions.
        return context.Database.ExecuteSqlRawAsync(sql);
#pragma warning restore EF1002
    }
}
