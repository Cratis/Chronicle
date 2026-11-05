// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ClosedStreams.for_ClosedStreamsConstraintStorage;

public class when_listing_within_a_scope_narrower_than_the_closed_streams_on_sqlite() : EventSourceIds.for_spelling_variants.given.a_spelling_storage(Sqlite())
{
    ClosedStream[] _all;
    ClosedStream[] _page;

    async Task Because()
    {
        await _harness.ClosedStreams.Close(new(new(EventSourceId: "alice", EventStreamType: "orders"), "owner", 1, null));
        await _harness.ClosedStreams.Close(new(new(EventSourceId: "alice", EventStreamType: "invoices"), "owner", 2, null));
        await _harness.ClosedStreams.Close(new(new(EventSourceId: "bob", EventStreamType: "orders"), "owner", 3, null));
        _all = [.. await _harness.ClosedStreams.GetAll(new(EventSourceId: "alice"))];
        _page = [.. await _harness.ClosedStreams.GetAll(new(EventSourceId: "alice"), 1, 1)];
    }

    [Fact] void should_list_every_closed_stream_of_the_event_source() => _all.Length.ShouldEqual(2);
    [Fact] void should_not_list_other_event_sources() => _all.Any(_ => _.Scope.EventSourceId == "bob").ShouldBeFalse();
    [Fact] void should_page_within_the_narrowed_list() => _page.Length.ShouldEqual(1);
}
