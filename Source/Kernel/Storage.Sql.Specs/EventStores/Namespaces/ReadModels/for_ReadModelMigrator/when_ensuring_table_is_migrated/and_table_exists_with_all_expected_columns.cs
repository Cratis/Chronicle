// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels.for_ReadModelMigrator.when_ensuring_table_is_migrated;

public class and_table_exists_with_all_expected_columns : given.a_read_model_migrator
{
    const string TableName = "books_already_current";

    IReadOnlyList<ProjectedColumn> _columns;
    IReadOnlySet<string> _actualColumns;

    async Task Establish()
    {
        _columns =
        [
            new ProjectedColumn("Id",    typeof(string), IsKey: true,  IsJson: false, IsArray: false, IsNullable: false),
            new ProjectedColumn("Title", typeof(string), IsKey: false, IsJson: false, IsArray: false, IsNullable: true),
            new ProjectedColumn(WellKnownProperties.LastHandledEventSequenceNumber, typeof(long?), IsKey: false, IsJson: false, IsArray: false, IsNullable: true)
        ];

        await using var context = CreateContext(TableName, _columns);
        await _migrator.EnsureTableMigrated(TableName, context);
    }

    async Task Because()
    {
        // Second call with the same schema — must be a no-op and must not throw.
        await using var context = CreateContext(TableName, _columns);
        await _migrator.EnsureTableMigrated(TableName, context);
        _actualColumns = await GetActualColumnNamesAsync(TableName);
    }

    [Fact] void should_retain_the_id_column() => _actualColumns.Contains("Id").ShouldBeTrue();
    [Fact] void should_retain_the_title_column() => _actualColumns.Contains("Title").ShouldBeTrue();
    [Fact] void should_retain_the_bookkeeping_column() => _actualColumns.Contains(WellKnownProperties.LastHandledEventSequenceNumber).ShouldBeTrue();
}
