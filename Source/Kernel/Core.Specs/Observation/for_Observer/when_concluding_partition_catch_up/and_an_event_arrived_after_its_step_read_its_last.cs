// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Observation.for_Observer.when_concluding_partition_catch_up;

/// <summary>
/// Partition B's step has read to event 9 while partition A's step reached 12. Event 11 for B was appended after B's
/// step read its last and dropped by live delivery, since B was still catching up. B must stay held back, so the step
/// reads on and handles it - it is below the position the observer moves to once the job reports back (#4583).
/// </summary>
public class and_an_event_arrived_after_its_step_read_its_last : given.an_observer_with_subscription_for_specific_event_type
{
    static readonly Key _partitionA = "partition-a";
    static readonly Key _partitionB = "partition-b";
    static readonly EventSequenceNumber _nextUnreadByB = 10UL;
    static readonly EventSequenceNumber _appendedForB = 11UL;

    bool _concluded;

    void Establish()
    {
        _stateStorage.State.CatchingUpPartitions.Add(_partitionA);
        _stateStorage.State.CatchingUpPartitions.Add(_partitionB);
        _eventSequence
            .GetNextSequenceNumberGreaterOrEqualTo(_nextUnreadByB, Arg.Any<IEnumerable<EventType>>(), Arg.Is<EventSourceId>(id => id.Value == "partition-b"))
            .Returns(_appendedForB);
        _eventSequence.ClearReceivedCalls();
    }

    async Task Because() => _concluded = await _observer.ConcludePartitionCatchUp(_partitionB, _nextUnreadByB, [event_type]);

    [Fact] void should_not_conclude_the_partition() => _concluded.ShouldBeFalse();
    [Fact] void should_keep_holding_back_live_events_for_the_partition() => _stateStorage.State.CatchingUpPartitions.ShouldContain(_partitionB);
    [Fact] void should_leave_the_other_partition_catching_up() => _stateStorage.State.CatchingUpPartitions.ShouldContain(_partitionA);
    [Fact] void should_look_for_unread_events_of_the_types_the_step_reads() => _eventSequence.Received(1).GetNextSequenceNumberGreaterOrEqualTo(_nextUnreadByB, Arg.Is<IEnumerable<EventType>>(types => types.SequenceEqual(new[] { event_type })), Arg.Any<EventSourceId>());
}
