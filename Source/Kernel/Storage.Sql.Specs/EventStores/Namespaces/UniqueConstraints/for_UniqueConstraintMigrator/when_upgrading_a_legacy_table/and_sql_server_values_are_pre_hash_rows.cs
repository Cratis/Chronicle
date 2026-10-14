// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.Sinks;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.UniqueConstraints.for_UniqueConstraintMigrator.when_upgrading_a_legacy_table;

[Collection(SqlServerCollection.Name)]
public class and_sql_server_values_are_pre_hash_rows(SqlServerFixture fixture) : given.a_legacy_server_table
{
    long _length;
    long _unique;
    string _savedValue;
    string[] _warnings;

    async Task Establish()
    {
        var connectionString = await fixture.CreateDatabase();
        await CreateLegacyTable(connectionString, true, () => fixture.DropDatabase(connectionString));
        await AddClaim("owner", "legacy@example.com");
    }

    async Task Because()
    {
        await _context.EnsureTableExists();
        _length = await ColumnLength();
        _unique = await IndexIsUnique();
        _savedValue = await _context.Entries.Select(entry => entry.Value).SingleAsync();
        _warnings = Warnings();
    }

    [Fact] void should_block_narrowing_even_if_the_row_fits_200_characters() => _length.ShouldEqual(-1);
    [Fact] void should_leave_the_unbounded_column_without_an_index() => _unique.ShouldEqual(-1);
    [Fact] void should_keep_the_legacy_value() => _savedValue.ShouldEqual("legacy@example.com");
    [Fact] void should_name_the_legacy_rows_issue_in_the_skip_warning() => _warnings.Single().ShouldContain("https://github.com/Cratis/Chronicle/issues/4866");
}
