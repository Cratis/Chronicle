// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Observation.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer;

public class when_releasing_a_refused_partition_replay_at_the_first_position : when_partition_replayed.given.an_observer_with_replaying_partition
{
    void Establish()
    {
        _stateStorage.State = _stateStorage.State with { LastHandledEventSequenceNumber = EventSequenceNumber.Unavailable, NextEventSequenceNumber = EventSequenceNumber.First };
        _eventSequence.GetNextSequenceNumberGreaterOrEqualTo(EventSequenceNumber.First, Arg.Any<IEnumerable<EventType>>(), Arg.Any<EventSourceId>()).Returns((EventSequenceNumber)10UL);
    }

    async Task Because() => await _observer.ReleaseRefusedPartitionReplay(_partition, EventSequenceNumber.First);

    [Fact] void should_preserve_the_absence_of_handled_progress() => _stateStorage.State.LastHandledEventSequenceNumber.ShouldEqual(EventSequenceNumber.Unavailable);
    [Fact] void should_preserve_the_first_position() => _stateStorage.State.NextEventSequenceNumber.ShouldEqual(EventSequenceNumber.First);
    [Fact] void should_probe_and_start_at_the_first_position() => _jobsManager.Received(1)
        .Start<ICatchUpObserverPartition, CatchUpObserverPartitionRequest>(Arg.Is<CatchUpObserverPartitionRequest>(_ => _.Key == _partition && _.FromSequenceNumber == EventSequenceNumber.First));
}
