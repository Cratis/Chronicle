// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Aggregates.for_ReducerAggregateRootStateProvider.given;

public class an_aggregate_root_that_handles_two_event_types_and_two_appended_events : an_aggregate_root_that_handles_two_event_types
{
    protected IImmutableList<AppendedEvent> _events;

    void Establish()
    {
        _events = new[]
        {
            AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(FirstEventType.EventTypeId, 42),
            AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(SecondEventType.EventTypeId, 47)
        }.ToImmutableList();
        _eventSequence.GetForEventSourceIdAndEventTypes(
            _eventSourceId,
            _eventTypes,
            _aggregateRootContext.EventStreamType,
            _aggregateRootContext.EventStreamId).Returns(_events);
    }
}
