// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Monads;
using Cratis.Orleans.Storage.Jobs;
using JobError = Cratis.Orleans.Storage.Jobs.JobError;

namespace Cratis.Chronicle.Observation.for_Observer.when_fencing_partition_replay;

public class and_job_lookup_throws_during_completion : given.an_observer_with_partition_replay_jobs
{
    void Establish()
    {
        _stateStorage.State.ReplayingPartitions.Add(_partition);
        _jobStorage.GetJobs<IReplayObserverPartition, JobState>()
            .Returns(Task.FromException<Catch<IImmutableList<JobState>, JobError>>(new InvalidOperationException("Storage unavailable")));
    }

    Task Because() => _observer.PartitionReplayed(_partition, 42UL, 42UL, [], _firstJob);

    [Fact] void should_keep_the_replay_fence() => _stateStorage.State.ReplayingPartitions.ShouldContain(_partition);
    [Fact] void should_not_persist_a_lifted_fence() => _storageStats.Writes.ShouldEqual(0);
}
