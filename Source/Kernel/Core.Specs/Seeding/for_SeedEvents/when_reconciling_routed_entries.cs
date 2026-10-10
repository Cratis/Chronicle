// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Seeding.for_SeedEvents;

public class when_reconciling_routed_entries : given.a_seeding_grain
{
    SeedEvents _request;

    void Establish()
    {
        var first = AnEntry("source", "type", "{}");
        first.EventSourceType = "Order";
        first.EventStreamType = "Lines";
        first.EventStreamId = "line-1";
        var second = AnEntry("source", "type", "{}");
        second.EventSourceType = "Order";
        second.EventStreamType = "Lines";
        second.EventStreamId = "line-2";
        _request = AGlobalRequestFor(first, second);
    }

    async Task Because() => await _request.Handle(_grainFactory);

    [Fact] void should_reconcile_both_groupings_without_losing_a_stream() => EntriesSeededGlobally.Count().ShouldEqual(2);
    [Fact] void should_keep_source_type() => EntriesSeededGlobally.All(_ => _.EventSourceType.Value == "Order").ShouldBeTrue();
    [Fact] void should_keep_stream_type() => EntriesSeededGlobally.All(_ => _.EventStreamType.Value == "Lines").ShouldBeTrue();
    [Fact] void should_keep_stream_ids() => EntriesSeededGlobally.Select(_ => _.EventStreamId.Value).ShouldContainOnly("line-1", "line-2");
}
