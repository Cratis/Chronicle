// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.when_completing_partition_replay;

public class and_it_is_reported_twice : given.a_partition_replay_covering_a_failure
{
    async Task Because()
    {
        await CompleteReplay();
        await CompleteReplay();
    }

    [Fact] void should_start_only_one_successor() => CheckSuccessor();
    [Fact] void should_decrement_the_count_only_once() => _completionStorage.PersistedState.FailedPartitionCount.ShouldEqual(FailedPartitionCount.Zero);
    [Fact] void should_resolve_the_episode_only_once() => _failedPartitionsStorage.State.ResolvedPartitions.Count().ShouldEqual(1);
    [Fact] void should_record_the_successor() => _completionStorage.PersistedState.CatchingUpPartitions.ShouldContain(_partition);
}
