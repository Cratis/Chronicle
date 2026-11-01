// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using KernelObserverState = Cratis.Chronicle.Storage.Observation.ObserverState;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Observers.for_ObserverStateStorage.when_round_tripping;

public class with_partition_replay_completions : given.an_observer_state_storage
{
    KernelObserverState _state;
    KernelObserverState _saved;
    KernelObserverState _cleared;
    KernelObserverState _legacy;

    void Establish() => _state = KernelObserverState.Empty with
    {
        Identifier = "observer",
        PartitionReplayCompletions =
        [
            new("partition", 42, 50, new(Guid.NewGuid()), DateTimeOffset.UnixEpoch),
            new("other", 60, 65, null, DateTimeOffset.UnixEpoch.AddSeconds(1))
        ]
    };

    async Task Because()
    {
        await _storage.Save(_state, Concepts.EventSequences.EventSequenceId.Log);
        _saved = await _storage.Get("observer");
        await _storage.Save(_state with { PartitionReplayCompletions = null }, Concepts.EventSequences.EventSequenceId.Log);
        _cleared = await _storage.Get("observer");
        await _storage.Save(KernelObserverState.Empty with { Identifier = "legacy" }, Concepts.EventSequences.EventSequenceId.Log);
        _legacy = await _storage.Get("legacy");
    }

    [Fact] void should_keep_all_completion_fields() => _saved.PartitionReplayCompletions.ShouldEqual(_state.PartitionReplayCompletions);
    [Fact] void should_clear_the_completed_intents() => _cleared.PartitionReplayCompletions.ShouldBeNull();
    [Fact] void should_keep_legacy_intents_null() => _legacy.PartitionReplayCompletions.ShouldBeNull();
}
