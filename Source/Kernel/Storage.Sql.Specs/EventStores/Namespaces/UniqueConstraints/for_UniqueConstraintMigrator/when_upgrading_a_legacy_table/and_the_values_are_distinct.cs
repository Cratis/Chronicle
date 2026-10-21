// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.UniqueConstraints.for_UniqueConstraintMigrator.when_upgrading_a_legacy_table;

public class and_the_values_are_distinct : given.a_legacy_sqlite_table
{
    bool _unique;
    int _rows;
    string _schemaBeforeSecondRun;
    string _schemaAfterSecondRun;

    async Task Establish() => await _context.Database.ExecuteSqlRawAsync("INSERT INTO legacy_claims VALUES ('owner', 'value', 42)");

    async Task Because()
    {
        await _context.EnsureTableExists();
        _unique = await IndexIsUnique();
        _rows = await _context.Entries.CountAsync();
        _schemaBeforeSecondRun = await ReadSchema();
        _migrator.ClearMigrationCache(_connection.ConnectionString);
        await _context.EnsureTableExists();
        _schemaAfterSecondRun = await ReadSchema();
    }

    async Task<string> ReadSchema()
    {
        await using var command = _connection.CreateCommand();
        command.CommandText = "SELECT sql FROM sqlite_master WHERE name = 'IX_legacy_claims_Value'";
        return (string)(await command.ExecuteScalarAsync())!;
    }

    [Fact] void should_make_the_index_unique() => _unique.ShouldBeTrue();
    [Fact] void should_keep_the_claim() => _rows.ShouldEqual(1);
    [Fact] void should_leave_the_second_run_unchanged() => _schemaAfterSecondRun.ShouldEqual(_schemaBeforeSecondRun);
}
