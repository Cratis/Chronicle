// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.Sinks;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.UniqueConstraints.for_UniqueConstraintMigrator.when_upgrading_a_legacy_table;

[Collection(SqlServerCollection.Name)]
public class and_sql_server_values_differ_only_in_case(SqlServerFixture fixture) : given.a_legacy_server_table
{
    long _unique;
    int _rows;
    string[] _warnings;

    async Task Establish()
    {
        var connectionString = await fixture.CreateDatabase();
        await CreateLegacyTable(connectionString, true, () => fixture.DropDatabase(connectionString));

        // This table is already indexable; case-only duplicates are judged by the server's collation.
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
        _rows = await _context.Entries.CountAsync();
        _warnings = Warnings();
    }

    [Fact] void should_fall_back_to_a_non_unique_index() => _unique.ShouldEqual(0);
    [Fact] void should_keep_both_case_variants() => _rows.ShouldEqual(2);
    [Fact] void should_warn_once_per_table_per_process() => _warnings.Length.ShouldEqual(1);
    [Fact] void should_name_the_table_and_duplicate_count() => _warnings.Single().ShouldContain($"{_tableName} has 1 duplicate values");
}
