// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Observation.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_fencing_partition_replay;

public class and_an_orphaned_fence_has_no_completion : given.an_observer_with_partition_replay_jobs
{
    void Establish()
    {
        _stateStorage.State.ReplayingPartitions.Add(_partition);
        _eventSequence.GetNextSequenceNumberGreaterOrEqualTo(EventSequenceNumber.First, Arg.Any<IEnumerable<EventType>>(), Arg.Any<Cratis.Chronicle.Concepts.Events.EventSourceId>()).Returns(EventSequenceNumber.First);
    }

    Task Because() => Crash();

    [Fact] void should_restart_from_first_rather_than_the_observers_global_cursor() => _jobsManager.Received(1)
        .Start<ICatchUpObserverPartition, CatchUpObserverPartitionRequest>(Arg.Is<CatchUpObserverPartitionRequest>(request => request.Key == _partition && request.FromSequenceNumber == EventSequenceNumber.First));
    [Fact] void should_release_the_replay_fence() => _stateStorage.State.ReplayingPartitions.ShouldNotContain(_partition);
}
