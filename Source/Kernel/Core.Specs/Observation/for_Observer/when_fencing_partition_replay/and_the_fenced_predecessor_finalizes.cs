// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;
using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.Jobs;
using JobError = Cratis.Orleans.Storage.Jobs.JobError;

namespace Cratis.Chronicle.Observation.for_Observer.when_fencing_partition_replay;

public class and_the_fenced_predecessor_finalizes : given.an_observer_with_partition_replay_jobs
{
    void Establish()
    {
        _stateStorage.State.ReplayingPartitions.Add(_partition);
        SetCatchupJobs(CatchupJob(_secondJob));
        EventSequenceHasNextEvent(48UL);
        _jobStorage.GetJob(_firstJob).Returns(Task.FromResult(Catch<JobState, JobError>.Failed(JobError.NotFound)));
    }

    async Task Because()
    {
        await _observer.PartitionReplayed(_partition, 42UL, 48UL, [], _firstJob);
        SetCatchupJobs(CatchupJob(_secondJob, JobStatus.CompletedSuccessfully));
        await RunRetryTimer();
        await Crash();
    }

    [Fact] void should_start_exactly_one_successor_from_the_furthest_read() => CheckStartedCatchupJob(48UL, _partition);
    [Fact] void should_remove_the_marker() => _stateStorage.State.ReplayingPartitions.ShouldNotContain(_partition);
    [Fact] void should_clear_the_completed_record() => _stateStorage.State.PartitionReplayCompletions!.ShouldBeEmpty();
}
