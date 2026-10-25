// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.EventSequences.for_AppendedEventsQueue.when_enqueuing;

public class with_subscriber_that_throws_on_first_event_and_second_event_is_enqueued : given.a_single_subscriber_with_an_event_type
{
    AppendedEvent _firstEvent;
    AppendedEvent _secondEvent;
    EventSourceId _firstPartition;
    EventSourceId _secondPartition;

    void Establish()
    {
        _firstPartition = Guid.NewGuid();
        _secondPartition = Guid.NewGuid();

        _firstEvent = AppendedEvent.Empty() with
        {
            Context = EventContext.Empty with
            {
                EventType = _eventType,
                EventSourceId = _firstPartition
            }
        };
        _secondEvent = AppendedEvent.Empty() with
        {
            Context = EventContext.Empty with
            {
                EventType = _eventType,
                EventSourceId = _secondPartition,
                SequenceNumber = 1UL
            }
        };

        _observer
            .When(_ => _.Handle(Arg.Is<Key>(k => k == (Key)_firstPartition), Arg.Any<IEnumerable<AppendedEvent>>()))
            .Do(_ => throw new Exception());
    }

    async Task Because()
    {
        await _queue.Enqueue([_firstEvent]);
        await _queue.Enqueue([_secondEvent]);
        await _queue.AwaitQueueDepletion();
    }

    [Fact] void should_still_process_the_second_event() => _handledEventsPerPartition.ContainsKey(_secondPartition).ShouldBeTrue();
}
