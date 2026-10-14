// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.UniqueConstraints.for_UniqueConstraintMigrator.when_upgrading_a_legacy_table;

public class and_values_already_have_multiple_owners : given.a_legacy_sqlite_table
{
    bool _unique;
    int _rows;
    string[] _warnings;

    async Task Establish() => await _context.Database.ExecuteSqlRawAsync("INSERT INTO legacy_claims VALUES ('first', 'shared', 42), ('second', 'shared', 43), ('third', 'shared', 44), ('fourth', 'another', 45), ('fifth', 'another', 46)");

    async Task Because()
    {
        await _context.EnsureTableExists();
        _migrator.ClearMigrationCache(_connection.ConnectionString);
        await _context.EnsureTableExists();
        _unique = await IndexIsUnique();
        _rows = await _context.Entries.CountAsync();
        _warnings = _logger.Warnings.ToArray();
    }

    [Fact] void should_keep_a_non_unique_index() => _unique.ShouldBeFalse();
    [Fact] void should_delete_nothing() => _rows.ShouldEqual(5);
    [Fact] void should_warn_once_per_table_per_process() => _warnings.Length.ShouldEqual(1);
    [Fact] void should_name_the_table() => _warnings.Single().ShouldContain(TableName);
    [Fact] void should_count_duplicate_values_not_losing_rows() => _warnings.Single().ShouldContain("2 duplicate values");
}
