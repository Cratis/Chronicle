// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.for_EventContextConverters;

public class when_converting_an_events_contract_without_an_event_source : Specification
{
    EventContext _result;

    void Because() => _result = new Contracts.Events.EventContext
    {
        EventType = new() { Id = "event", Generation = 1 },
        EventSourceId = "source",
        EventStore = "store",
        Namespace = "namespace",
        EventStreamType = EventStreamType.All,
        EventStreamId = EventStreamId.Default,
        EventSourceType = EventSourceType.Default,
        Causation = [],
        CausedBy = new(),
        Tags = []
    }.ToClient();

    [Fact] void should_resolve_the_event_source_as_not_set() => _result.EventSource.ShouldEqual(EventSourceName.NotSet);
}
