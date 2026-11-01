// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Observation.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_completing_partition_replay;

public class and_a_full_replay_completes : given.a_partition_replay_covering_a_failure
{
    void Establish() => _failureStorage.FailOnWrite = 1;

    async Task Because()
    {
        await CompleteWithFault();
        await _observer.Replayed(60UL);
        await _observer.RunWatchdogAsync();
    }

    [Fact] void should_remove_the_in_memory_completion() => _observer.IsCompletingPartitionReplay(_partition).ShouldBeFalse();
    [Fact] void should_clear_the_persisted_intent() => _completionStorage.PersistedState.PartitionReplayCompletions.ShouldBeNull();
    [Fact] void should_not_start_the_old_successor() => _jobsManager.DidNotReceive()
        .Start<ICatchUpObserverPartition, CatchUpObserverPartitionRequest>(Arg.Is<CatchUpObserverPartitionRequest>(_ =>
            _.Key == _partition && _.FromSequenceNumber == (EventSequenceNumber)51UL));
}
