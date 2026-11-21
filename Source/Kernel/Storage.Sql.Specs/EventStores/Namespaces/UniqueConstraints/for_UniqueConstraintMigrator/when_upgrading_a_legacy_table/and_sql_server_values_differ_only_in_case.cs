// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.Sinks;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.UniqueConstraints.for_UniqueConstraintMigrator.when_upgrading_a_legacy_table;

[Collection(SqlServerCollection.Name)]
public class and_sql_server_values_differ_only_in_case(SqlServerFixture fixture) : given.a_legacy_server_table
{
    long _unique;
    UniqueConstraintIndexEntry[] _rows;
    string[] _warnings;

    async Task Establish()
    {
        var connectionString = await fixture.CreateDatabase();
        await CreateLegacyTable(connectionString, true, () => fixture.QueueDatabaseDrop(connectionString));
#pragma warning disable EF1002 // Fixture-owned identifiers delimited by the provider.
        await _context.Database.ExecuteSqlRawAsync($"ALTER TABLE {_table} ALTER COLUMN {_value} NVARCHAR(200) COLLATE Latin1_General_100_CI_AS NOT NULL");
#pragma warning restore EF1002
        await AddClaim("first", new string('a', 64));
        await AddClaim("second", new string('A', 64));
    }

    async Task Because()
    {
        await _context.EnsureTableExists();
        _migrator.ClearMigrationCache(_connectionString);
        await _context.EnsureTableExists();
        _unique = await IndexIsUnique();
        _rows = await _context.Entries.AsNoTracking().OrderBy(row => row.EventSourceId).ToArrayAsync();
        _warnings = Warnings();
    }

    [Fact] void should_install_a_unique_value_index() => _unique.ShouldEqual(1);
    [Fact] void should_preserve_both_case_variants() => _rows.Select(row => row.Value).ShouldEqual([new string('a', 64), new string('A', 64)]);
    [Fact] void should_preserve_each_owner() => _rows.Select(row => row.EventSourceId).ShouldEqual(["first", "second"]);
    [Fact] void should_preserve_each_sequence_number() => _rows.Select(row => row.SequenceNumber).ShouldEqual([42m, 42m]);
    [Fact] void should_not_warn_about_ordinally_distinct_values() => _warnings.ShouldBeEmpty();
}
