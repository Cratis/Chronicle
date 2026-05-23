// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.EventSequences.for_AppendedEventsQueue.when_enqueuing.with_event_redacted;

public class and_original_event_type_is_in_subscription : given.a_single_subscriber_with_event_redacted_event_type
{
    AppendedEvent _appendedEvent;
    EventSourceId _eventSourceId;

    void Establish()
    {
        _eventSourceId = Guid.NewGuid();
        dynamic content = new ExpandoObject();
        content.originalEventType = _subscribedEventType.Id.Value;
        _appendedEvent = AppendedEvent.Empty() with
        {
            Context = EventContext.Empty with
            {
                EventType = _eventRedactedEventType,
                EventSourceId = _eventSourceId
            },
            Content = content
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
