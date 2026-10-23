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
    /// Ensures a declared index, using the canonical ordering shadow for SQLite decimals.
    /// </summary>
    /// <param name="context">The read-model context.</param>
    /// <param name="column">The declared scalar column.</param>
    /// <param name="online">Whether the provider should build a native index online.</param>
    /// <returns>An awaitable task.</returns>
    internal static async Task Ensure(ReadModelDbContext context, ProjectedColumn column, bool online)
    {
        if (context.Database.IsSqlite() && SqliteDecimalSortKey.IsDecimal(column))
        {
            await EnsureDecimalShadow(context, column);
            return;
        }
        await ReadModelIndexes.Ensure(context, context.TableName, column.Name, online);
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
