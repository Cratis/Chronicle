// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.Sinks;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.UniqueConstraints.for_UniqueConstraintMigrator.when_upgrading_a_legacy_table;

[Collection(PostgreSqlCollection.Name)]
public class and_postgresql_values_are_distinct(PostgreSqlFixture fixture) : given.a_legacy_server_table
{
    long _unique;
    long _secondUnique;
    int _rows;

    async Task Establish()
    {
        // The table fits PostgreSQL's limit, but its index name is truncated by the server.
        _tableName += "_with_long_value_index";
        var connectionString = await fixture.CreateDatabase();
        await CreateLegacyTable(connectionString, false, () => fixture.DropDatabase(connectionString));
        await AddClaim("owner", new string('a', 64));
    }

    async Task Because()
    {
        await _context.EnsureTableExists();
        _unique = await IndexIsUnique();
        _migrator.ClearMigrationCache(_connectionString);
        await _context.EnsureTableExists();
        _secondUnique = await IndexIsUnique();
        _rows = await _context.Entries.CountAsync();
    }

    [Fact] void should_upgrade_to_a_unique_index() => _unique.ShouldEqual(1);
    [Fact] void should_leave_the_index_unique_on_a_second_run() => _secondUnique.ShouldEqual(1);
    [Fact] void should_keep_every_row() => _rows.ShouldEqual(1);
}
