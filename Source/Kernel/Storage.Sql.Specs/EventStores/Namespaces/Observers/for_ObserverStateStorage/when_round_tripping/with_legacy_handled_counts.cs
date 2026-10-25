// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore;

using KernelObserverState = Cratis.Chronicle.Storage.Observation.ObserverState;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Observers.for_ObserverStateStorage.when_round_tripping;

public class with_legacy_handled_counts : given.an_observer_state_storage
{
    KernelObserverState _saved;

    async Task Because()
    {
        await _storage.Save(new() { Identifier = "observer" }, Concepts.EventSequences.EventSequenceId.Log);
        await using var context = CreateContext();
        await context.Database.ExecuteSqlRawAsync("UPDATE Observers SET HandledEventCountPerEventType = NULL, HandledEventCount = 0 WHERE Id = 'observer'");
        _saved = await _storage.Get("observer");
    }

    [Fact] void should_read_a_zero_handled_event_count() => _saved.HandledEventCount.Value.ShouldEqual(0UL);
    [Fact] void should_read_an_empty_dictionary() => _saved.HandledEventCountPerEventType.ShouldBeEmpty();
    [Fact] void should_not_read_a_null_dictionary() => _saved.HandledEventCountPerEventType.ShouldNotBeNull();
}
