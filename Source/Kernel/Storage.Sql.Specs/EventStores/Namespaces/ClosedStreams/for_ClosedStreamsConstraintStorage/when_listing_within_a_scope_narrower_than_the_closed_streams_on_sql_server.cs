// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Storage.Sql.Sinks;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ClosedStreams.for_ClosedStreamsConstraintStorage;

[Collection(SqlServerCollection.Name)]
public class when_listing_within_a_scope_narrower_than_the_closed_streams_on_sql_server(SqlServerFixture fixture) : EventSourceIds.for_spelling_variants.given.a_spelling_storage(new("sql_server", fixture.CreateDatabase, fixture.DropDatabase))
{
    ClosedStream[] _all;
    ClosedStream[] _page;
    ClosedStream[] _upper;

    async Task Because()
    {
        await _harness.ClosedStreams.Close(new(new(EventSourceId: "alice", EventStreamType: "orders"), "owner", 1, null));
        await _harness.ClosedStreams.Close(new(new(EventSourceId: "alice", EventStreamType: "invoices"), "owner", 2, null));
        await _harness.ClosedStreams.Close(new(new(EventSourceId: "Alice", EventStreamType: "orders"), "owner", 3, null));
        _all = [.. await _harness.ClosedStreams.GetAll(new(EventSourceId: "alice"))];
        _page = [.. await _harness.ClosedStreams.GetAll(new(EventSourceId: "alice"), 1, 1)];
        _upper = [.. await _harness.ClosedStreams.GetAll(new(EventSourceId: "Alice"))];
    }

    [Fact] void should_list_every_closed_stream_of_the_exact_event_source() => _all.Length.ShouldEqual(2);
    [Fact] void should_not_list_another_spelling() => _all.Any(_ => _.Scope.EventSourceId == "Alice").ShouldBeFalse();
    [Fact] void should_page_within_the_exact_list() => _page.Length.ShouldEqual(1);
    [Fact] void should_list_the_other_spelling_on_its_own() => _upper.Length.ShouldEqual(1);
}
