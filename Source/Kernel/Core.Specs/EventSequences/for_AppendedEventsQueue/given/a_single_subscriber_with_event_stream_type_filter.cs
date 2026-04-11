// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.EventSequences.for_AppendedEventsQueue.given;

public class a_single_subscriber_with_event_stream_type_filter : a_single_subscriber
{
    protected EventType _eventType = new("Some event", 1);
    protected EventStreamType _filteredEventStreamType = new("invoices");

    protected override IEnumerable<EventType> EventTypes => [_eventType];

    protected override ObserverFilters? Filters =>
        new([], EventStreamType: _filteredEventStreamType);
}
