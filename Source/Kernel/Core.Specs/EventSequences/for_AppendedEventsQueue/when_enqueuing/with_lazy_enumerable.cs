// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.EventSequences.for_AppendedEventsQueue.when_enqueuing;

public class with_lazy_enumerable : given.a_single_subscriber_with_an_event_type
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
        var lazyEvents = new[] { _appendedEvent }.Where(_ => true);
        await _queue.Enqueue(lazyEvents);
        await _queue.AwaitQueueDepletion();
    }

    [Fact] void should_handle_event_once() => _handledEventsPerPartition[_eventSourceId].Count.ShouldEqual(1);
    [Fact] void should_handle_correct_event() => _handledEventsPerPartition[_eventSourceId][0].Events.ShouldContainOnly(_appendedEvent);
}
