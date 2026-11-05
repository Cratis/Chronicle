// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.Sinks;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSourceIds.for_spelling_variants.when_reading_events;

[Collection(SqlServerCollection.Name)]
public class and_source_types_differ_on_sql_server(SqlServerFixture fixture) : given.a_spelling_storage(new("sql_server", fixture.CreateDatabase, fixture.DropDatabase))
{
    ulong[] _plain;
    ulong[] _upper;
    ulong[] _padded;
    ulong[] _fullWidth;
    ulong[] _accented;

    async Task Because()
    {
        await _harness.Append("alice-1", "Account");
        await _harness.Append("alice-2", "account");
        await _harness.Append("alice-3", "Account ");
        await _harness.Append("alice-4", "\uFF21ccount");
        await _harness.Append("alice-5", "Acc\u00F3unt");
        _plain = await _harness.ReadBySourceType("Account");
        _upper = await _harness.ReadBySourceType("account");
        _padded = await _harness.ReadBySourceType("Account ");
        _fullWidth = await _harness.ReadBySourceType("\uFF21ccount");
        _accented = await _harness.ReadBySourceType("Acc\u00F3unt");
    }

    [Fact] void should_return_only_the_events_of_the_exact_type() => _plain.ShouldEqual([0UL]);
    [Fact] void should_return_only_the_events_of_the_lower_case_type() => _upper.ShouldEqual([1UL]);
    [Fact] void should_return_only_the_events_of_the_type_with_a_trailing_space() => _padded.ShouldEqual([2UL]);
    [Fact] void should_return_only_the_events_of_the_full_width_type() => _fullWidth.ShouldEqual([3UL]);
    [Fact] void should_return_only_the_events_of_the_accented_type() => _accented.ShouldEqual([4UL]);
}
