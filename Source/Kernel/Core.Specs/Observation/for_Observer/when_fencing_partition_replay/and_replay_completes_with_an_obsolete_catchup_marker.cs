// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_Observer.when_fencing_partition_replay;

public class and_replay_completes_with_an_obsolete_catchup_marker : given.an_observer_with_partition_replay_jobs
{
    void Establish()
    {
        _stateStorage.State.ReplayingPartitions.Add(_partition);
        _stateStorage.State.CatchingUpPartitions.Add(_partition);
        EventSequenceDoesNotHaveNextEvent(42UL);
    }

    Task Because() => _observer.PartitionReplayed(_partition, 42UL, 42UL, [], _firstJob);

    [Fact] void should_release_replay_ownership() => _stateStorage.State.ReplayingPartitions.ShouldNotContain(_partition);
    [Fact] void should_release_the_obsolete_catchup_marker() => _stateStorage.State.CatchingUpPartitions.ShouldNotContain(_partition);
    [Fact] void should_not_start_unnecessary_catchup() => CheckDidNotStartCatchupJob();
    [Fact] void should_persist_the_released_markers() => _storageStats.Writes.ShouldBeGreaterThan(0);
}
