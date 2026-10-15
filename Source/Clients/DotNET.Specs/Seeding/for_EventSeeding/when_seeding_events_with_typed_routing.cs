// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSources;

namespace Cratis.Chronicle.Seeding.for_EventSeeding;

public class when_seeding_events_with_typed_routing : given.a_seeding_builder
{
    void Establish() => _eventSources.GetFor(typeof(Order)).Returns(new EventSourceDefinition(typeof(Order), "Order", "", ConcurrencyDimensions.None, [new EventStream("Lines", "", ConcurrencyDimensions.None)]));

    async Task Because()
    {
        _seeding.ForEvents([new EventForEventSourceId("source", new TestEvent("value"))
        {
            EventSource = typeof(Order),
            EventStream = "Lines",
            EventStreamId = "line-1",
            Tags = ["dynamic-tag", "static-tag"]
        }]);
        await _seeding.Register();
    }

    [Fact] void should_resolve_source_type() => _request.GlobalByEventSource.Single().Entries.Single().EventSourceType.ShouldEqual("Order");
    [Fact] void should_resolve_stream_type() => _request.GlobalByEventSource.Single().Entries.Single().EventStreamType.ShouldEqual("Lines");
    [Fact] void should_keep_stream_id() => _request.GlobalByEventSource.Single().Entries.Single().EventStreamId.ShouldEqual("line-1");
    [Fact] void should_merge_and_deduplicate_tags() => _request.GlobalByEventSource.Single().Entries.Single().Tags.ShouldContainOnly("dynamic-tag", "static-tag");

    class Order;
}
