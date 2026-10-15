// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.Sinks;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.UniqueConstraints.for_UniqueConstraintMigrator.when_upgrading_a_legacy_table;

[Collection(SqlServerCollection.Name)]
public class and_sql_server_values_are_hashes(SqlServerFixture fixture) : given.a_legacy_server_table
{
    long _unique;
    long _length;
    long _secondUnique;
    long _secondLength;
    int _rows;

    async Task Establish()
    {
        var connectionString = await fixture.CreateDatabase();
        await CreateLegacyTable(connectionString, true, () => fixture.DropDatabase(connectionString));
        await AddClaim("owner", new string('a', 64));
    }

    async Task Because()
    {
        await _context.EnsureTableExists();
        _unique = await IndexIsUnique();
        _length = await ColumnLength();
        _migrator.ClearMigrationCache(_connectionString);
        await _context.EnsureTableExists();
        _secondUnique = await IndexIsUnique();
        _secondLength = await ColumnLength();
        _rows = await _context.Entries.CountAsync();
    }

    [Fact] void should_create_a_unique_index() => _unique.ShouldEqual(1);
    [Fact] void should_bound_the_column_to_200_characters() => _length.ShouldEqual(200);
    [Fact] void should_keep_the_index_on_a_second_run() => _secondUnique.ShouldEqual(_unique);
    [Fact] void should_not_widen_the_column_on_a_second_run() => _secondLength.ShouldEqual(_length);
    [Fact] void should_keep_every_row() => _rows.ShouldEqual(1);
}
