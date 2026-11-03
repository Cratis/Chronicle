// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore;

#pragma warning disable CA2100 // Specification-owned identifiers.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels.for_ReadModelMigrator.when_reading_columns_of_a_hostile_table;

/// <summary>
/// The table name closes the quoted name of the PRAGMA reading its columns and appends a statement dropping another table.
/// </summary>
public class and_the_name_breaks_out_of_a_pragma : given.a_read_model_migrator
{
    const string Hostile = "x\"); DROP TABLE canary; --";

    IReadOnlyList<ProjectedColumn> _columns;
    IReadOnlySet<string> _canaryColumns;
    Exception _error;

    async Task Establish()
    {
        _columns =
        [
            new ProjectedColumn("Id", typeof(string), IsKey: true, IsJson: false, IsArray: false, IsNullable: false),
            new ProjectedColumn(WellKnownProperties.LastHandledEventSequenceNumber, typeof(long?), IsKey: false, IsJson: false, IsArray: false, IsNullable: true)
        ];
        await using var context = CreateContext("canary", _columns);
        await context.Database.ExecuteSqlRawAsync("CREATE TABLE \"canary\" (\"Id\" integer NOT NULL)");
    }

    async Task Because()
    {
        await using var context = CreateContext(Hostile, _columns);
        _error = await Catch.Exception(() => _migrator.EnsureTableMigrated(Hostile, context));
        _canaryColumns = await GetActualColumnNamesAsync("canary");
    }

    [Fact] void should_not_fail() => _error.ShouldBeNull();
    [Fact] void should_keep_the_canary() => _canaryColumns.Contains("Id").ShouldBeTrue();
}
