// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.for_NamespaceDbContext;

public class when_migrating : given.a_migrated_namespace_database
{
    IEnumerable<string> _missing;

    void Because()
    {
        using var context = CreateContext();
        _missing = context.Model.GetEntityTypes()
            .Where(entityType => entityType.GetTableName() is not null)
            .SelectMany(entityType =>
            {
                var table = entityType.GetTableName()!;
                var columns = ColumnsOf(table).ToHashSet(StringComparer.Ordinal);
                if (columns.Count == 0)
                {
                    return [$"table {table}"];
                }

                var storeObject = Microsoft.EntityFrameworkCore.Metadata.StoreObjectIdentifier.Table(table, entityType.GetSchema());
                return entityType.GetProperties()
                    .Select(property => property.GetColumnName(storeObject))
                    .Where(column => column is not null && !columns.Contains(column))
                    .Select(column => $"column {table}.{column}");
            })
            .ToArray();
    }

    [Fact] void should_create_every_table_and_column_the_model_maps() => string.Join(", ", _missing).ShouldEqual(string.Empty);
}
