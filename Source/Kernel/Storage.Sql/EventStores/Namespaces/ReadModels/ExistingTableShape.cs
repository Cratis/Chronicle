// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;

/// <summary>
/// Decides whether a table that already exists has the shape the read model has now, so a read never has to change the table.
/// </summary>
static class ExistingTableShape
{
    /// <summary>
    /// Determines whether an existing table has every column the read model expects.
    /// </summary>
    /// <param name="context">The context describing the shape the read model has now.</param>
    /// <param name="existingColumns">The names of the columns the table has.</param>
    /// <returns>True only when the keys, the projected columns, the shadow columns and the initialized marker are all there; an older or mismatched table is not read.</returns>
    internal static bool IsComplete(ReadModelDbContext context, IReadOnlySet<string> existingColumns) =>
        context.Columns
            .Select(column => column.Name)
            .Concat(ShadowColumns.For(context).Select(column => column.Name))
            .All(existingColumns.Contains);
}
