// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation.Jobs;
using Cratis.Monads;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_fencing_partition_replay;

public class and_starting_the_job_returns_an_error : given.an_observer_with_partition_replay_jobs
{
    void Establish() => _jobsManager.Start<IReplayObserverPartition, ReplayObserverPartitionRequest>(Arg.Any<ReplayObserverPartitionRequest>())
        .Returns(Result<JobId, StartJobError>.Failed(StartJobError.Unknown));

    Task Because() => _observer.ReplayPartition(_partition);

    [Fact] void should_persist_the_fence_and_its_rollback() => _storageStats.Writes.ShouldEqual(2);
    [Fact] async Task should_release_the_fence() => (await _observer.IsPartitionFenced(_partition, _firstJob)).ShouldBeFalse();
}
