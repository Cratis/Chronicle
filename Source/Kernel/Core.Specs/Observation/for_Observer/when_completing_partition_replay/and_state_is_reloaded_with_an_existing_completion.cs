// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.when_completing_partition_replay;

public class and_state_is_reloaded_with_an_existing_completion : given.a_partition_replay_covering_a_failure
{
    void Establish() => _failureStorage.FailOnWrite = 1;

    async Task Because()
    {
        await CompleteWithFault();
        _completionStorage.State = _completionStorage.PersistedState with
        {
            PartitionReplayCompletions = [new(_partition, 90UL, 100UL, null, DateTimeOffset.UnixEpoch)]
        };
        await _completionStorage.WriteStateAsync();
        await _observer.Subscribe<NullObserverSubscriber>(ObserverType.Reactor, [_eventType], SiloAddress.Zero);
    }

    [Fact] void should_reapply_the_in_memory_completion_after_routing() => CheckSuccessor();
    [Fact] void should_clear_the_persisted_intent() => _completionStorage.PersistedState.PartitionReplayCompletions.ShouldBeNull();
}
