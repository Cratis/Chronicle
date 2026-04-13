// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.EventSequences.for_AppendedEventsQueue.when_enqueuing.with_event_stream_type_filter;

public class and_event_has_non_matching_event_stream_type : given.a_single_subscriber_with_event_stream_type_filter
{
    AppendedEvent _appendedEvent;
    EventSourceId _eventSourceId;

    void Establish()
    {
        _eventSourceId = Guid.NewGuid();
        _appendedEvent = AppendedEvent.Empty() with
        {
            Context = EventContext.Empty with
            {
                EventType = _eventType,
                EventSourceId = _eventSourceId,
                EventStreamType = new EventStreamType("purchases")
            }
        };
    }

    async Task Because()
    {
        await _queue.Enqueue([_appendedEvent]);
        await _queue.AwaitQueueDepletion();
    }

    [Fact] void should_not_dispatch_the_event_to_the_observer() => _handledEventsPerPartition.ShouldBeEmpty();
}
