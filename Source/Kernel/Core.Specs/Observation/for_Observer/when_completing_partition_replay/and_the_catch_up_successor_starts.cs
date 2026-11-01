// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_Observer.when_completing_partition_replay;

public class and_the_catch_up_successor_starts : given.a_partition_replay_covering_a_failure
{
    async Task Because() => await CompleteReplay();

    [Fact] void should_start_the_successor() => CheckSuccessor();
    [Fact] void should_clear_the_persisted_intent() => _completionStorage.PersistedState.PartitionReplayCompletions.ShouldBeNull();
    [Fact] void should_persist_the_catch_up_marker() => _completionStorage.PersistedState.CatchingUpPartitions.ShouldContain(_partition);
}
