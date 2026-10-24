// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation.Jobs;
using Cratis.Monads;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_fencing_partition_replay;

public class and_starting_the_job_throws : given.an_observer_with_partition_replay_jobs
{
    Exception? _exception;

    void Establish() => _jobsManager.Start<IReplayObserverPartition, ReplayObserverPartitionRequest>(Arg.Any<ReplayObserverPartitionRequest>())
        .Returns(Task.FromException<Result<JobId, StartJobError>>(new InvalidOperationException("start failed")));

    async Task Because() => _exception = await Cratis.Specifications.Catch.Exception(() => _observer.ReplayPartition(_partition));

    [Fact] void should_preserve_the_failure() => _exception!.Message.ShouldEqual("start failed");
    [Fact] void should_persist_the_fence_and_its_rollback() => _storageStats.Writes.ShouldEqual(2);
    [Fact] async Task should_release_the_fence() => (await _observer.IsPartitionFenced(_partition, _firstJob)).ShouldBeFalse();
}
