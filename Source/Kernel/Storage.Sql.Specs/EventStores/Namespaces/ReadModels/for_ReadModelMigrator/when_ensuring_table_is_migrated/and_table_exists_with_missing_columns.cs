// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels.for_ReadModelMigrator.when_ensuring_table_is_migrated;

/// <summary>
/// Reproduces the "column b.Title does not exist" bug where two read models share the same
/// container name across namespaces. The Lending read model creates the "Books" table with an
/// "Available" column; when the Inventory read model later calls EnsureTableMigrated expecting a
/// "Title" column the migrator must detect the gap and ALTER TABLE ADD COLUMN rather than crashing.
/// </summary>
public class and_table_exists_with_missing_columns : given.a_read_model_migrator
{
    const string TableName = "books_shared_container";

    IReadOnlyList<ProjectedColumn> _inventoryColumns;
    IReadOnlySet<string> _actualColumns;

    async Task Establish()
    {
        // Lending read model creates the "Books" table first — only Id and Available columns.
        IReadOnlyList<ProjectedColumn> lendingColumns =
        [
            new ProjectedColumn("Id",        typeof(string), IsKey: true,  IsJson: false, IsArray: false, IsNullable: false),
            new ProjectedColumn("Available", typeof(int?),   IsKey: false, IsJson: false, IsArray: false, IsNullable: true),
            new ProjectedColumn(WellKnownProperties.LastHandledEventSequenceNumber, typeof(long?), IsKey: false, IsJson: false, IsArray: false, IsNullable: true)
        ];

        await using var lendingContext = CreateContext(TableName, lendingColumns);
        await _migrator.EnsureTableMigrated(TableName, lendingContext);

        // Inventory read model expects Id and Title — Title is absent from the existing table.
        _inventoryColumns =
        [
            new ProjectedColumn("Id",    typeof(string), IsKey: true,  IsJson: false, IsArray: false, IsNullable: false),
            new ProjectedColumn("Title", typeof(string), IsKey: false, IsJson: false, IsArray: false, IsNullable: true),
            new ProjectedColumn(WellKnownProperties.LastHandledEventSequenceNumber, typeof(long?), IsKey: false, IsJson: false, IsArray: false, IsNullable: true)
        ];
    }

    async Task Because()
    {
        await using var inventoryContext = CreateContext(TableName, _inventoryColumns);
        await _migrator.EnsureTableMigrated(TableName, inventoryContext);
        _actualColumns = await GetActualColumnNamesAsync(TableName);
    }

    [Fact] void should_add_the_missing_title_column() => _actualColumns.Contains("Title").ShouldBeTrue();
    [Fact] void should_preserve_the_id_column() => _actualColumns.Contains("Id").ShouldBeTrue();
    [Fact] void should_preserve_the_pre_existing_available_column() => _actualColumns.Contains("Available").ShouldBeTrue();
    [Fact] void should_preserve_the_bookkeeping_column() => _actualColumns.Contains(WellKnownProperties.LastHandledEventSequenceNumber).ShouldBeTrue();
}
