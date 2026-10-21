// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.Sinks;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.UniqueConstraints.for_UniqueConstraintMigrator.when_upgrading_a_legacy_table;

[Collection(SqlServerCollection.Name)]
public class and_sql_server_values_exceed_200_characters(SqlServerFixture fixture) : given.a_legacy_server_table
{
    readonly string _oversizedValue = new string('a', 200) + " ";
    string[] _values;
    long _length;
    long _unique;
    string[] _warnings;

    async Task Establish()
    {
        var connectionString = await fixture.CreateDatabase();
        await CreateLegacyTable(connectionString, true, () => fixture.DropDatabase(connectionString));
        await AddClaim("first", _oversizedValue);
        await AddClaim("second", _oversizedValue);
    }

    async Task Because()
    {
        await _context.EnsureTableExists();
        _length = await ColumnLength();
        _unique = await IndexIsUnique();
        _values = await _context.Entries.Select(entry => entry.Value).ToArrayAsync();
        _warnings = Warnings();
    }

    [Fact] void should_leave_the_column_unbounded() => _length.ShouldEqual(-1);
    [Fact] void should_not_create_an_index_on_an_unbounded_column() => _unique.ShouldEqual(-1);
    [Fact] void should_keep_every_value_without_truncating_even_trailing_spaces() => _values.ShouldContainOnly(_oversizedValue, _oversizedValue);
    [Fact] void should_count_the_oversized_rows() => _warnings.ShouldContain(message => message.Contains("2 rows exceed 200 characters", StringComparison.Ordinal));
    [Fact] void should_also_warn_about_duplicate_values() => _warnings.ShouldContain(message => message.Contains($"{_tableName} has 1 duplicate values", StringComparison.Ordinal));
}
