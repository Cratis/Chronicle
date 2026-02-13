// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Grains.EventSequences.for_AppendedEventsQueue.when_enqueuing;

public class single_event_with_single_subscriber : given.a_single_subscriber_with_an_event_type
{
    AppendedEvent _appendedEvent;

    EventSourceId _eventSourceId;

    void Establish()
    {
        _appendedEvent = AppendedEvent.Empty();
        _eventSourceId = Guid.NewGuid();
        _appendedEvent = _appendedEvent with
        {
            Context = EventContext.Empty with
            {
                EventType = _eventType,
                EventSourceId = _eventSourceId
            }
        };
    }

    async Task Because()
    {
        await _queue.Enqueue([_appendedEvent]);
        await _queue.AwaitQueueDepletion();
    }

    [Fact] void should_call_handle_on_observer_once() => _handledEventsPerPartition[_eventSourceId].Count.ShouldEqual(1);
    [Fact] void should_call_handle_on_observer_with_correct_event_source_id() => _handledEventsPerPartition[_eventSourceId][0].Partition.Value.ShouldEqual(_eventSourceId.Value);
    [Fact] void should_call_handle_on_observer_with_correct_event() => _handledEventsPerPartition[_eventSourceId][0].Events.ShouldContainOnly(_appendedEvent);
}
