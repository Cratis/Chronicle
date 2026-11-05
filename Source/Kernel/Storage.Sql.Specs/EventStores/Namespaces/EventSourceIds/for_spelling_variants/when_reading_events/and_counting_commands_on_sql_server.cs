// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.Sinks;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSourceIds.for_spelling_variants.when_reading_events;

[Collection(SqlServerCollection.Name)]
public class and_counting_commands_on_sql_server(SqlServerFixture fixture) : given.a_spelling_storage(new("sql_server", fixture.CreateDatabase, fixture.DropDatabase))
{
    int _afterAppend;
    int _afterRead;

    async Task Because()
    {
        await _harness.Append("Alice");
        var beforeAppend = _harness.Counter.Count;
        await _harness.Append("Alice");
        _afterAppend = _harness.Counter.Count - beforeAppend;
        var beforeRead = _harness.Counter.Count;
        await _harness.Read("Alice");
        _afterRead = _harness.Counter.Count - beforeRead;
    }

    [Fact] void should_append_with_the_baseline_number_of_commands() => _afterAppend.ShouldEqual(2);
    [Fact] void should_read_with_the_baseline_number_of_commands() => _afterRead.ShouldEqual(2);
}
