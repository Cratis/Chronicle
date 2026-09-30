// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.EventSequences.for_AppendedEventsQueue.when_enqueuing;

public class event_of_a_type_the_subscriber_never_named_with_subscriber_of_all_event_types : given.a_single_subscriber
{
    AppendedEvent _appendedEvent;
    EventSourceId _eventSourceId;

    protected override IEnumerable<EventType> EventTypes => [];

    async Task Establish()
    {
        await _queue.SubscribeToAllEventTypes(_observerKey);

        _eventSourceId = Guid.NewGuid();
        _appendedEvent = AppendedEvent.Empty() with
        {
            Context = EventContext.Empty with
            {
                EventType = new("An event type registered after subscribing", 1),
                EventSourceId = _eventSourceId
            }
        };
    }

    async Task Because()
    {
        await _queue.Enqueue([_appendedEvent]);
        await _queue.AwaitQueueDepletion();
    }

    [Fact] void should_deliver_the_event_to_the_subscriber() => _handledEventsPerPartition[_eventSourceId][0].Events.ShouldContainOnly(_appendedEvent);
}
