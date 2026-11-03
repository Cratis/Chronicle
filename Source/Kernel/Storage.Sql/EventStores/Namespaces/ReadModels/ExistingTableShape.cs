// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;

/// <summary>
/// Decides what of a table that already exists can be read as it is, so a read never has to change the table.
/// </summary>
static class ExistingTableShape
{
    /// <summary>
    /// Gets the columns of the read model that an existing table has.
    /// </summary>
    /// <param name="context">The context describing the shape the read model has now.</param>
    /// <param name="existingColumns">The names of the columns the table has.</param>
    /// <returns>The columns to read, or null when the table lacks a column reads depend on: the key, the initialized marker or a shadow column.</returns>
    internal static IReadOnlyList<ProjectedColumn>? Serve(ReadModelDbContext context, IReadOnlySet<string> existingColumns)
    {
        var required = context.Columns
            .Where(column => column.IsKey || column.Name == WellKnownProperties.ReadModelInstanceInitialized)
            .Select(column => column.Name)
            .Concat(ShadowColumns.For(context).Select(column => column.Name));

        return required.All(existingColumns.Contains)
            ? [.. context.Columns.Where(column => existingColumns.Contains(column.Name))]
            : null;
    }
}
