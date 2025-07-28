// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Grains.EventSequences.for_AppendedEventsQueue.when_enqueuing;

public class with_subscriber_that_has_unsubscribed : given.a_single_subscriber_with_an_event_type
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

        _queue.Unsubscribe(_observerKey);
    }

    async Task Because()
    {
        await _queue.Enqueue([_appendedEvent]);
        await _queue.AwaitQueueDepletion();
    }

    [Fact] void should_not_handle_any_events() => _handledEventsPerPartition.Count.ShouldEqual(0);
}
