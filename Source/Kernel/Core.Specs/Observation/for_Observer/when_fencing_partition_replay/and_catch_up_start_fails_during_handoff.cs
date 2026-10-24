// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation.Jobs;
using Cratis.Monads;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_fencing_partition_replay;

public class and_catch_up_start_fails_during_handoff : given.an_observer_with_partition_replay_jobs
{
    bool _held;
    bool _notStarted;

    void Establish()
    {
        _stateStorage.State.ReplayingPartitions.Add(_partition);
        EventSequenceHasNextEvent(48UL);
        _jobsManager.Start<ICatchUpObserverPartition, CatchUpObserverPartitionRequest>(Arg.Any<CatchUpObserverPartitionRequest>())
            .Returns(Result<JobId, StartJobError>.Failed(StartJobError.Unknown));
    }

    async Task Because()
    {
        await _observer.PartitionReplayed(_partition, 42UL, 48UL, [], _firstJob);
        _held = _stateStorage.State.ReplayingPartitions.Contains(_partition) && _stateStorage.State.PartitionReplayCompletions!.Any(record => record.HandingOff && !record.HandedOff);
        _notStarted = !_stateStorage.State.CatchingUpPartitions.Contains(_partition);
        _jobsManager.ClearReceivedCalls();
        _jobsManager.Start<ICatchUpObserverPartition, CatchUpObserverPartitionRequest>(Arg.Any<CatchUpObserverPartitionRequest>())
            .Returns(Result<JobId, StartJobError>.Success(JobId.New()));
        await RunRetryTimer();
    }

    [Fact] void should_keep_the_record_and_replay_fence_until_start_succeeds() => _held.ShouldBeTrue();
    [Fact] void should_not_leave_failed_catchup_ownership() => _notStarted.ShouldBeTrue();
    [Fact] void should_retry_the_same_handoff() => CheckStartedCatchupJob(48UL, _partition);
    [Fact] void should_lift_the_fence_after_success() => _stateStorage.State.ReplayingPartitions.ShouldNotContain(_partition);
}
