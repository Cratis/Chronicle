// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Monads;
using Cratis.Orleans.Storage.Jobs;
using JobError = Cratis.Orleans.Storage.Jobs.JobError;

namespace Cratis.Chronicle.Observation.for_Observer.when_fencing_partition_replay;

public class and_predecessor_lookup_returns_an_error : given.an_observer_with_partition_replay_jobs
{
    void Establish()
    {
        _stateStorage.State.ReplayingPartitions.Add(_partition);
        _jobStorage.GetJobs<ICatchUpObserverPartition, JobState>()
            .Returns(Task.FromResult(Catch<IImmutableList<JobState>, JobError>.Failed(JobError.NotFound)));
    }

    Task Because() => _observer.PartitionReplayed(_partition, 42UL, 48UL, [], _firstJob);

    [Fact] void should_keep_the_fence() => _stateStorage.State.ReplayingPartitions.ShouldContain(_partition);
    [Fact] void should_not_prepare_the_record() => _stateStorage.State.PartitionReplayCompletions!.Single().HandingOff.ShouldBeFalse();
    [Fact] void should_not_start_a_successor() => CheckDidNotStartCatchupJob();
    [Fact] void should_schedule_a_retry() => (RetryTimerIndex >= 0).ShouldBeTrue();
}
