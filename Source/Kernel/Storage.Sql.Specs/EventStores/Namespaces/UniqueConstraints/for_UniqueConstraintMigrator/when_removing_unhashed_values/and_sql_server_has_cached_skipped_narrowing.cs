// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.Sinks;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.UniqueConstraints.for_UniqueConstraintMigrator.when_removing_unhashed_values;

[Collection(SqlServerCollection.Name)]
public class and_sql_server_has_cached_skipped_narrowing(SqlServerFixture fixture) : given.a_legacy_server_table
{
    long _deleted;
    readonly string _hashShapedRawValue = new('a', 64);

    async Task Establish()
    {
        var connectionString = await fixture.CreateDatabase();
        await CreateLegacyTable(connectionString, true, () => fixture.DropDatabase(connectionString));
#pragma warning disable EF1002 // The fixture delimits its own table name.
        await _context.Database.ExecuteSqlRawAsync($"ALTER TABLE {_table} ALTER COLUMN [Value] NVARCHAR(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL");
#pragma warning restore EF1002
        await AddClaim("raw", "legacy@example.com");
        await AddClaim("uppercase", new string('A', 64));
        await AddClaim("hash-shaped-raw", _hashShapedRawValue);
        await AddClaim("trailing-space", _hashShapedRawValue + " ");
        await _context.EnsureTableExists();
        (await ColumnLength()).ShouldEqual(-1L);
    }

    async Task Because() => _deleted = await UnhashedUniqueConstraintValues.RemoveFrom(_context, _tableName, CancellationToken.None);

    [Fact] void should_delete_uppercase_and_unhashed_rows_even_under_case_insensitive_collation() => _deleted.ShouldEqual(3L);
    [Fact] async Task should_keep_a_hash_shaped_raw_value() => (await _context.Entries.Select(row => row.Value).SingleAsync()).ShouldEqual(_hashShapedRawValue);
    [Fact] async Task should_narrow_even_though_the_migrator_cached_the_skip() => (await ColumnLength()).ShouldEqual(200L);
    [Fact] async Task should_create_the_unique_value_index() => (await IndexIsUnique()).ShouldEqual(1L);
    [Fact] async Task should_be_idempotent() => (await UnhashedUniqueConstraintValues.RemoveFrom(_context, _tableName, CancellationToken.None)).ShouldEqual(0L);
}
