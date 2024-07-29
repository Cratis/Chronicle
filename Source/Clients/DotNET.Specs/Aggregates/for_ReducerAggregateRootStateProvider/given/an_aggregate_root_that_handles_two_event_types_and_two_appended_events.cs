// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reducers;

namespace Cratis.Chronicle.Aggregates.for_ReducerAggregateRootStateProvider.given;

public class an_aggregate_root_that_handles_two_event_types_and_two_appended_events : an_aggregate_root_that_handles_two_event_types
{
    protected IImmutableList<AppendedEvent> events;

    void Establish()
    {
        events = new[]
        {
            AppendedEvent.EmptyWithEventType(FirstEventType.EventTypeId),
            AppendedEvent.EmptyWithEventType(SecondEventType.EventTypeId)
        }.ToImmutableList();
        _eventSequence.GetForEventSourceIdAndEventTypes(_eventSourceId, _eventTypes).Returns(events);
    }
}
