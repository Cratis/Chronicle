// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels.for_ReadModelMigrator.when_ensuring_table_is_migrated;

public class and_table_does_not_exist : given.a_read_model_migrator
{
    const string TableName = "books_inventory";

    IReadOnlyList<ProjectedColumn> _columns;
    IReadOnlySet<string> _actualColumns;

    void Establish()
    {
        _columns =
        [
            new ProjectedColumn("Id",    typeof(string), IsKey: true,  IsJson: false, IsArray: false, IsNullable: false),
            new ProjectedColumn("Title", typeof(string), IsKey: false, IsJson: false, IsArray: false, IsNullable: true),
            new ProjectedColumn(WellKnownProperties.LastHandledEventSequenceNumber, typeof(long?), IsKey: false, IsJson: false, IsArray: false, IsNullable: true)
        ];
    }

    async Task Because()
    {
        await using var context = CreateContext(TableName, _columns);
        await _migrator.EnsureTableMigrated(TableName, context);
        _actualColumns = await GetActualColumnNamesAsync(TableName);
    }

    [Fact] void should_create_the_id_column() => _actualColumns.Contains("Id").ShouldBeTrue();
    [Fact] void should_create_the_title_column() => _actualColumns.Contains("Title").ShouldBeTrue();
    [Fact] void should_create_the_bookkeeping_column() => _actualColumns.Contains(WellKnownProperties.LastHandledEventSequenceNumber).ShouldBeTrue();
}
