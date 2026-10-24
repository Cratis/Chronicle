// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_fencing_partition_replay;

public class and_several_fenced_predecessors_finalize_separately : given.an_observer_with_partition_replay_jobs
{
    readonly JobId _thirdJob = JobId.New();
    bool _waitedForTheLastPredecessor;

    void Establish()
    {
        _stateStorage.State.ReplayingPartitions.Add(_partition);
        SetCatchupJobs(CatchupJob(_secondJob), CatchupJob(_thirdJob));
        EventSequenceHasNextEvent(48UL);
    }

    async Task Because()
    {
        await _observer.PartitionReplayed(_partition, 42UL, 48UL, [], _firstJob);
        SetCatchupJobs(CatchupJob(_secondJob, JobStatus.CompletedSuccessfully), CatchupJob(_thirdJob));
        await RunRetryTimer();
        CheckDidNotStartCatchupJob();
        _waitedForTheLastPredecessor = _stateStorage.State.ReplayingPartitions.Contains(_partition)
            && !_stateStorage.State.PartitionReplayCompletions!.Single().HandingOff;
        SetCatchupJobs(CatchupJob(_secondJob, JobStatus.CompletedSuccessfully), CatchupJob(_thirdJob, JobStatus.CompletedWithFailures));
        await RunRetryTimer();
    }

    [Fact] void should_wait_for_every_predecessor() => _waitedForTheLastPredecessor.ShouldBeTrue();
    [Fact] void should_start_one_successor_only_after_all_finalize() => CheckStartedCatchupJob(48UL, _partition);
    [Fact] void should_lift_the_marker_after_draining() => _stateStorage.State.ReplayingPartitions.ShouldNotContain(_partition);
}
