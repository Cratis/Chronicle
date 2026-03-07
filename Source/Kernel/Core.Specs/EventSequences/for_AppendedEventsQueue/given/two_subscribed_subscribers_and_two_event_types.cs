// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.EventSequences.for_AppendedEventsQueue.given;

public class two_subscribed_subscribers_and_two_event_types : two_subscribed_subscribers
{
    protected EventType _firstEventType = new("Some event", 1);
    protected EventType _secondEventType = new("Some other event", 2);
    protected AppendedEvent _firstAppendedEvent;
    protected AppendedEvent _secondAppendedEvent;
    protected EventSourceId _firstEventSourceId;
    protected EventSourceId _secondEventSourceId;

    void Establish()
    {
        _firstAppendedEvent = AppendedEvent.Empty();
        _firstEventSourceId = Guid.NewGuid();
        _firstAppendedEvent = _firstAppendedEvent with
        {
            Context = EventContext.Empty with
            {
                EventType = _firstEventType,
                EventSourceId = _firstEventSourceId
            }
        };

        _secondAppendedEvent = AppendedEvent.Empty();
        _secondEventSourceId = Guid.NewGuid();
        _secondAppendedEvent = _secondAppendedEvent with
        {
            Context = EventContext.Empty with
            {
                EventType = _secondEventType,

                EventSourceId = _secondEventSourceId
            }
        };
    }

    protected override IEnumerable<EventType> EventTypes => [_firstEventType, _secondEventType];
}
