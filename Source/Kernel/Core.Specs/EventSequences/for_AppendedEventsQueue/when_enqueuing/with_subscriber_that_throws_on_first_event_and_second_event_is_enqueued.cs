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
                EventSourceId = _firstPartition,
                SequenceNumber = 0UL
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
        // Both partitions share one dispatch, independently of enqueue coalescing timing.
        await _queue.Enqueue([_firstEvent, _secondEvent]);
        await _queue.AwaitQueueDepletion();
    }

    [Fact] void should_attempt_the_first_event() =>
        _observer.Received(1).Handle(Arg.Is<Key>(key => key == (Key)_firstPartition), Arg.Any<IEnumerable<AppendedEvent>>());

    [Fact] void should_not_deliver_the_second_event_live_past_the_hole() => _handledEventsPerPartition.ContainsKey(_secondPartition).ShouldBeFalse();
    [Fact] void should_recover_the_gap_through_catch_up() => _observer.Received(1).CatchUp();
}
