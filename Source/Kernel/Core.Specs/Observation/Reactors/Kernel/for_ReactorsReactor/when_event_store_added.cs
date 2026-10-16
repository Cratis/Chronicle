// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.Reactors.Kernel.for_ReactorsReactor;

public class when_event_store_added : given.a_reactors_reactor
{
    EventStoreAdded _event;
    EventContext _context;
    EventStoreName _eventStoreName;

    void Establish()
    {
        _eventStoreName = new EventStoreName("new-store");
        _event = new EventStoreAdded(_eventStoreName);
        _context = EventContext.From(
            EventStoreName.System,
            EventStoreNamespaceName.Default,
            EventType.Unknown,
            EventSourceType.Default,
            new EventSourceId("event-store-added"),
            EventStreamType.All,
            EventStreamId.Default,
            EventSequenceNumber.First,
            new CorrelationId(Guid.NewGuid()));
    }

    async Task Because() => await _reactor.EventStoreAdded(_event, _context);

    [Fact] void should_discover_and_register_event_types_for_the_event_store() =>
        _eventTypes.Received(1).DiscoverAndRegister(_eventStoreName);

    [Fact] void should_discover_and_register_reactors_for_the_event_store() =>
        _reactors.Received(1).DiscoverAndRegister(_eventStoreName, EventStoreNamespaceName.Default);
}
