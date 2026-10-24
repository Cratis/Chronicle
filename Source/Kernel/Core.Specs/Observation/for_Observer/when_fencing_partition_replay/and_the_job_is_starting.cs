// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation.Jobs;
using Cratis.Monads;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_fencing_partition_replay;

public class and_the_job_is_starting : given.an_observer_with_partition_replay_jobs
{
    bool _fencedAtStart;
    int _writesAtStart;

    void Establish() => _jobsManager.Start<IReplayObserverPartition, ReplayObserverPartitionRequest>(Arg.Any<ReplayObserverPartitionRequest>())
        .Returns(async _ =>
        {
            _writesAtStart = _storageStats.Writes;
            _fencedAtStart = await _observer.IsPartitionFenced(_partition, _firstJob);
            return Result<JobId, StartJobError>.Success(_firstJob);
        });

    Task Because() => _observer.ReplayPartition(_partition);

    [Fact] void should_fence_delivery_before_starting_the_job() => _fencedAtStart.ShouldBeTrue();
    [Fact] void should_persist_the_fence_before_starting_the_job() => _writesAtStart.ShouldBeGreaterThan(0);
    [Fact] async Task should_leave_other_partitions_open() => (await _observer.IsPartitionFenced(_otherPartition, _firstJob)).ShouldBeFalse();
}
