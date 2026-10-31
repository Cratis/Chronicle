// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using KernelObserverState = Cratis.Chronicle.Storage.Observation.ObserverState;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Observers.for_ObserverStateStorage.when_round_tripping;

public class with_running_state_changed_at : given.an_observer_state_storage
{
    readonly DateTimeOffset _timestamp = new(2026, 1, 2, 3, 4, 5, TimeSpan.Zero);
    KernelObserverState _saved;
    KernelObserverState _legacy;
    KernelObserverState _renamed;
    KernelObserverState _updated;

    async Task Because()
    {
        await _storage.Save(new() { Identifier = "observer", RunningStateChangedAt = _timestamp });
        _saved = await _storage.Get("observer");
        await _storage.Save(_saved with { RunningStateChangedAt = _timestamp.AddHours(1) });
        _updated = await _storage.Get("observer");
        await _storage.Rename("observer", "renamed");
        _renamed = await _storage.Get("renamed");
        await _storage.Save(new() { Identifier = "legacy" });
        _legacy = await _storage.Get("legacy");
    }

    [Fact] void should_keep_the_timestamp() => _saved.RunningStateChangedAt.ShouldEqual(_timestamp);
    [Fact] void should_update_an_existing_timestamp() => _updated.RunningStateChangedAt.ShouldEqual(_timestamp.AddHours(1));
    [Fact] void should_keep_the_timestamp_on_rename() => _renamed.RunningStateChangedAt.ShouldEqual(_updated.RunningStateChangedAt);
    [Fact] void should_leave_the_legacy_timestamp_unknown() => _legacy.RunningStateChangedAt.ShouldBeNull();
}
