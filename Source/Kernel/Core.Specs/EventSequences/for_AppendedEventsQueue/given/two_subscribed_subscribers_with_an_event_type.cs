// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.EventSequences.for_AppendedEventsQueue.given;

public class two_subscribed_subscribers_with_an_event_type : two_subscribed_subscribers
{
    protected EventType _eventType = new("Some event", 1);
    protected AppendedEvent _appendedEvent;
    protected EventSourceId _eventSourceId;

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

    protected override IEnumerable<EventType> EventTypes => [_eventType];
}
