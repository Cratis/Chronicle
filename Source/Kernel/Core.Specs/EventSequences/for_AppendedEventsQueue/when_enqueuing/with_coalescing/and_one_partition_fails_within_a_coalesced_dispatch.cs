// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.EventSequences.for_AppendedEventsQueue.when_enqueuing.with_coalescing;

/// <summary>
/// A coalesced dispatch may hold several partitions. If one partition's delivery fails, the failure is contained to
/// that partition group so the remaining partitions in the same dispatch still deliver — the failed one is recovered
/// through the observer's own partition-failure and catch-up machinery.
/// </summary>
public class and_one_partition_fails_within_a_coalesced_dispatch : given.a_single_subscriber_with_an_event_type
{
    AppendedEvent _failingPartitionEvent;
    AppendedEvent _healthyPartitionEvent;
    EventSourceId _failingPartition;
    EventSourceId _healthyPartition;

    void Establish()
    {
        _failingPartition = Guid.NewGuid();
        _healthyPartition = Guid.NewGuid();

        _failingPartitionEvent = AppendedEvent.Empty() with
        {
            Context = EventContext.Empty with
            {
                EventType = _eventType,
                EventSourceId = _failingPartition,
                SequenceNumber = 0UL
            }
        };
        _healthyPartitionEvent = AppendedEvent.Empty() with
        {
            Context = EventContext.Empty with
            {
                EventType = _eventType,
                EventSourceId = _healthyPartition,
                SequenceNumber = 1UL
            }
        };

        _observer
            .When(_ => _.Handle(Arg.Is<Key>(key => key == (Key)_failingPartition), Arg.Any<IEnumerable<AppendedEvent>>()))
            .Do(_ => throw new Exception());
    }

    async Task Because()
    {
        await _queue.Enqueue([_failingPartitionEvent, _healthyPartitionEvent]);
        await _queue.AwaitQueueDepletion();
    }

    [Fact] void should_attempt_the_failing_partition() =>
        _observer.Received().Handle(Arg.Is<Key>(key => key == (Key)_failingPartition), Arg.Any<IEnumerable<AppendedEvent>>());

    [Fact] void should_still_deliver_the_healthy_partition() =>
        _handledEventsPerPartition.ContainsKey(_healthyPartition).ShouldBeTrue();
}
