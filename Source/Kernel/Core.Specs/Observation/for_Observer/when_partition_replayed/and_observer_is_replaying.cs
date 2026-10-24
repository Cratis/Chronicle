// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.when_partition_replayed;

public class and_observer_is_replaying : given.an_observer_with_replaying_partition
{
    EventSequenceNumber _newSequenceNumber;

    void Establish()
    {
        _newSequenceNumber = _lastHandledEventSequenceNumber.Next();
        _stateStorage.State = _stateStorage.State with { RunningState = ObserverRunningState.Replaying };
    }

    async Task Because() => await _observer.PartitionReplayed(_partition, _newSequenceNumber);

    [Fact] void should_write_state_once() => _storageStats.Writes.ShouldEqual(1);
    [Fact] void should_retain_the_partition_fence_until_full_replay_finishes() => _stateStorage.State.ReplayingPartitions.ShouldContain(_partition);
    [Fact] void should_not_replace_the_full_replay_position_with_partial_progress() => _stateStorage.State.LastHandledEventSequenceNumber.ShouldEqual(_lastHandledEventSequenceNumber);
    [Fact] void should_record_partial_progress_for_later_reconciliation() => _stateStorage.State.PartitionReplayCompletions!.Single().LastHandledEventSequenceNumber.ShouldEqual(_newSequenceNumber);
    [Fact] void should_not_start_catchup_job() => CheckDidNotStartCatchupJob();
}
