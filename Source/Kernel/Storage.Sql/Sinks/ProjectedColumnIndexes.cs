// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.Sinks;

/// <summary>
/// Selects the physical index representation for a projected scalar column.
/// </summary>
internal static class ProjectedColumnIndexes
{
    /// <summary>
    /// Ensures a declared index, using the canonical ordering shadow for a single ascending SQLite decimal column.
    /// </summary>
    /// <param name="context">The read-model context.</param>
    /// <param name="table">The table name.</param>
    /// <param name="plan">The resolved index plan.</param>
    /// <param name="online">Whether the provider should build a native index online.</param>
    /// <returns>An awaitable task.</returns>
    internal static async Task Ensure(ReadModelDbContext context, string table, ReadModelIndexPlan plan, bool online)
    {
        if (context.Database.IsSqlite() && plan.IsLegacy && SqliteDecimalSortKey.IsDecimal(plan.Targets[0].Column))
        {
            await EnsureDecimalShadow(context, plan.Targets[0].Column);
            return;
        }
        await ReadModelIndexes.Ensure(context, table, plan, online);
    }

    static async Task EnsureDecimalShadow(ReadModelDbContext context, ProjectedColumn column)
    {
        await ShadowColumns.Access(context, SqliteDecimalSortKey.ColumnNameFor(column.Name), async () =>
        {
            await ShadowColumnIndexes.Ensure(context);
            return true;
        });
        await ShadowColumnIndexes.RemoveLegacyDecimalIndex(context, column.Name);
    }
}
