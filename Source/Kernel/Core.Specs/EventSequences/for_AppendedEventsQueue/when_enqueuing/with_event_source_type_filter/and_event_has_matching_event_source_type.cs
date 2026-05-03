// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.EventSequences.for_AppendedEventsQueue.when_enqueuing.with_event_source_type_filter;

public class and_event_has_matching_event_source_type : given.a_single_subscriber_with_event_source_type_filter
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
                EventSourceType = _filteredEventSourceType
            }
        };
    }

    async Task Because()
    {
        await _queue.Enqueue([_appendedEvent]);
        await _queue.AwaitQueueDepletion();
    }

    [Fact] void should_dispatch_the_event_to_the_observer() => _handledEventsPerPartition[(Key)_eventSourceId].Count.ShouldEqual(1);
    [Fact] void should_pass_the_correct_event() => _handledEventsPerPartition[(Key)_eventSourceId][0].Events.ShouldContainOnly(_appendedEvent);
}
