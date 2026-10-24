// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_fencing_partition_replay;

public class and_the_observer_reactivates : given.an_observer_with_partition_replay_jobs
{
    void Establish()
    {
        _stateStorage.State.ReplayingPartitions.Add(_partition);
        _stateStorage.State.ReplayingPartitions.Add(_otherPartition);
        _jobs = ImmutableList.Create(ReplayJob(_firstJob, _partition), ReplayJob(_secondJob, _otherPartition, JobStatus.CompletedSuccessfully));
    }

    Task Because() => Crash(resetStorageStatistics: false);

    [Fact] async Task should_keep_the_running_replay_fenced()
    {
        _stateStorage.State.ReplayingPartitions.ShouldContain(_partition);
        (await _observer.IsPartitionFenced(_partition, _secondJob)).ShouldBeTrue();
    }
    [Fact] void should_remove_the_orphaned_marker() => _stateStorage.State.ReplayingPartitions.Contains(_otherPartition).ShouldBeFalse();
    [Fact] async Task should_lift_the_orphaned_fence() => (await _observer.IsPartitionFenced(_otherPartition, _firstJob)).ShouldBeFalse();
    [Fact] void should_persist_reconciliation() => _storageStats.Writes.ShouldBeGreaterThan(0);
}
