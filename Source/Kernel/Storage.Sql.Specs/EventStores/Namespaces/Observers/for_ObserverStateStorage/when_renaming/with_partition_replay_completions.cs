// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using KernelObserverState = Cratis.Chronicle.Storage.Observation.ObserverState;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Observers.for_ObserverStateStorage.when_renaming;

public class with_partition_replay_completions : given.an_observer_state_storage
{
    KernelObserverState _state;
    KernelObserverState _saved;

    async Task Establish()
    {
        _state = KernelObserverState.Empty with
        {
            Identifier = "observer",
            PartitionReplayCompletions =
            [
                new("partition", 42, 50, new(Guid.NewGuid()), DateTimeOffset.UnixEpoch),
                new("other", 60, 65, null, DateTimeOffset.UnixEpoch.AddSeconds(1))
            ]
        };
        await _storage.Save(_state, Concepts.EventSequences.EventSequenceId.Log);
    }

    async Task Because()
    {
        await _storage.Rename("observer", "renamed");
        _saved = await _storage.Get("renamed");
    }

    [Fact] void should_keep_all_completion_fields() => _saved.PartitionReplayCompletions.ShouldEqual(_state.PartitionReplayCompletions);
    [Fact] void should_rename_the_observer() => _saved.Identifier.ShouldEqual((Concepts.Observation.ObserverId)"renamed");
}
