// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.when_completing_partition_replay;

public class and_a_stale_intent_is_restored : given.a_partition_replay_covering_a_failure
{
    bool _restored;

    async Task Establish()
    {
        _completionStorage.State = _completionStorage.PersistedState with
        {
            ReplayingPartitions = new HashSet<Key>(),
            PartitionReplayCompletions = [new(_partition, 42UL, 50UL, null, DateTimeOffset.UnixEpoch)]
        };
        await _completionStorage.WriteStateAsync();
    }

    async Task Because()
    {
        await Crash();
        _restored = _observer.IsCompletingPartitionReplay(_partition);
        await _observer.Subscribe<NullObserverSubscriber>(ObserverType.Reactor, [_eventType], SiloAddress.Zero);
    }

    [Fact] void should_not_restore_the_stale_completion() => _restored.ShouldBeFalse();
    [Fact] void should_drop_the_stale_persisted_intent() => _completionStorage.PersistedState.PartitionReplayCompletions.ShouldBeNull();
}
