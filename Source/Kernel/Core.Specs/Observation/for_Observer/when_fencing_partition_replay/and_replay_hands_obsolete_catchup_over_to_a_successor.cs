// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_Observer.when_fencing_partition_replay;

public class and_replay_hands_obsolete_catchup_over_to_a_successor : given.an_observer_with_partition_replay_jobs
{
    void Establish()
    {
        _stateStorage.State.ReplayingPartitions.Add(_partition);
        _stateStorage.State.CatchingUpPartitions.Add(_partition);
        EventSequenceHasNextEvent(42UL);
    }

    Task Because() => _observer.PartitionReplayed(_partition, 42UL, 42UL, [], _firstJob);

    [Fact] void should_start_catchup_after_the_replay_position() => CheckStartedCatchupJob(42UL, _partition);
    [Fact] void should_release_the_replay_fence() => _stateStorage.State.ReplayingPartitions.ShouldNotContain(_partition);
    [Fact] void should_register_the_successor_as_catching_up() => _stateStorage.State.CatchingUpPartitions.ShouldContain(_partition);
}
