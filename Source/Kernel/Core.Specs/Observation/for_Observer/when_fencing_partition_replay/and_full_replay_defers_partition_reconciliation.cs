// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.when_fencing_partition_replay;

public class and_full_replay_defers_partition_reconciliation : given.an_observer_with_partition_replay_jobs
{
    void Establish()
    {
        _stateStorage.State = _stateStorage.State with { RunningState = ObserverRunningState.Replaying };
        _stateStorage.State.ReplayingPartitions.Add(_partition);
        EventSequenceHasNextEvent(48UL);
    }

    async Task Because()
    {
        await _observer.PartitionReplayed(_partition, 42UL, 48UL, [], _firstJob);
        await RunRetryTimer();
    }

    [Fact] void should_keep_the_replay_fence() => _stateStorage.State.ReplayingPartitions.ShouldContain(_partition);
    [Fact] void should_persist_the_completion_for_later() => _stateStorage.State.PartitionReplayCompletions!.Single().HandingOff.ShouldBeFalse();
    [Fact] void should_not_start_partition_catchup() => CheckDidNotStartCatchupJob();
}
