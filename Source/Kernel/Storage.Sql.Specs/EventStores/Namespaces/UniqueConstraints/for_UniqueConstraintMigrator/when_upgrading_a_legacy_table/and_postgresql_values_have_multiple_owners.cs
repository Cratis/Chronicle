// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.Sinks;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.UniqueConstraints.for_UniqueConstraintMigrator.when_upgrading_a_legacy_table;

[Collection(PostgreSqlCollection.Name)]
public class and_postgresql_values_have_multiple_owners(PostgreSqlFixture fixture) : given.a_legacy_server_table
{
    long _unique;
    int _rows;
    string[] _warnings;
    long _repairedUnique;

    async Task Establish()
    {
        var connectionString = await fixture.CreateDatabase();
        await CreateLegacyTable(connectionString, false, () => fixture.DropDatabase(connectionString));
        await AddClaim("first", new string('a', 64));
        await AddClaim("second", new string('a', 64));
    }

    async Task Because()
    {
        await _context.EnsureTableExists();
        _migrator.ClearMigrationCache(_connectionString);
        await _context.EnsureTableExists();
        _unique = await IndexIsUnique();
        _rows = await _context.Entries.CountAsync();
        _warnings = Warnings();
        await _context.Entries.Where(entry => entry.EventSourceId == "second").ExecuteDeleteAsync();
        _migrator.ClearMigrationCache(_connectionString);
        await _context.EnsureTableExists();
        _repairedUnique = await IndexIsUnique();
    }

    [Fact] void should_fall_back_to_a_non_unique_index() => _unique.ShouldEqual(0);
    [Fact] void should_delete_nothing() => _rows.ShouldEqual(2);
    [Fact] void should_warn_once_per_process() => _warnings.Length.ShouldEqual(1);
    [Fact] void should_name_the_table_and_duplicate_count() => _warnings.Single().ShouldContain($"{_tableName} has 1 duplicate values");
    [Fact] void should_retry_and_upgrade_after_an_operator_repairs_the_claims() => _repairedUnique.ShouldEqual(1);
}
