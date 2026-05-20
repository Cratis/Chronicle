// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer;

public class when_replaying_partition_and_observer_is_replaying : given.an_observer
{
    static Key _partition;

    void Establish()
    {
        _partition = "some-partition";
        _stateStorage.State = _stateStorage.State with { RunningState = ObserverRunningState.Replaying };
        _storageStats.ResetCounts();
    }

    Task Because() => _observer.ReplayPartitionTo(_partition, EventSequenceNumber.Max);

    [Fact] void should_not_add_partition_to_replaying_partitions() => _stateStorage.State.ReplayingPartitions.ShouldNotContain(_partition);
    [Fact] void should_not_start_replay_observer_partition_job() => _jobsManager.DidNotReceive().Start<IReplayObserverPartition, ReplayObserverPartitionRequest>(Arg.Any<ReplayObserverPartitionRequest>());
    [Fact] void should_not_write_state() => _storageStats.Writes.ShouldEqual(0);
}
