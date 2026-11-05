// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.Sinks;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSourceIds.for_spelling_variants.when_reading_events;

[Collection(SqlServerCollection.Name)]
public class on_sql_server(SqlServerFixture fixture) : given.a_spelling_storage(new("sql_server", fixture.CreateDatabase, fixture.DropDatabase))
{
    ulong[] _first;
    ulong[] _upper;
    ulong[] _padded;

    async Task Because()
    {
        await _harness.Append("Alice");
        await _harness.Append("Alice");
        await _harness.Append("alice");
        await _harness.Append("Alice ");
        _first = await _harness.Read("Alice");
        _upper = await _harness.Read("alice");
        _padded = await _harness.Read("Alice ");
    }

    [Fact] void should_return_only_the_events_of_the_exact_id() => _first.ShouldEqual([0UL, 1UL]);
    [Fact] void should_return_only_the_events_of_the_lower_case_id() => _upper.ShouldEqual([2UL]);
    [Fact] void should_return_only_the_events_of_the_id_with_a_trailing_space() => _padded.ShouldEqual([3UL]);
}
