// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_Observer.when_completing_partition_replay;

public class and_no_catch_up_is_needed : given.a_partition_replay_covering_a_failure
{
    void Establish() => EventSequenceDoesNotHaveNextEvent(50UL);

    async Task Because() => await CompleteReplay();

    [Fact] void should_not_start_a_successor() => CheckDidNotStartCatchupJob();
    [Fact] void should_clear_the_persisted_intent() => _completionStorage.PersistedState.PartitionReplayCompletions.ShouldBeNull();
    [Fact] void should_release_the_replay_marker() => _completionStorage.PersistedState.ReplayingPartitions.ShouldNotContain(_partition);
}
