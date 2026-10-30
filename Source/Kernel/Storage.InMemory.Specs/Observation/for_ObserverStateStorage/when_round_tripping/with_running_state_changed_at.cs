// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Storage.InMemory.Observation.for_ObserverStateStorage.when_round_tripping;

public class with_running_state_changed_at : Specification
{
    readonly DateTimeOffset _timestamp = new(2026, 1, 2, 3, 4, 5, TimeSpan.Zero);
    ObserverStateStorage _storage;
    ObserverState _saved;
    ObserverState _renamed;
    ObserverState _legacy;

    void Establish() => _storage = new();

    async Task Because()
    {
        await _storage.Save(new() { Identifier = "observer", RunningStateChangedAt = _timestamp });
        _saved = await _storage.Get("observer");
        await _storage.Rename("observer", "renamed");
        _renamed = await _storage.Get("renamed");
        _legacy = await _storage.Get("legacy");
    }

    [Fact] void should_keep_the_timestamp() => _saved.RunningStateChangedAt.ShouldEqual(_timestamp);
    [Fact] void should_keep_the_timestamp_on_rename() => _renamed.RunningStateChangedAt.ShouldEqual(_timestamp);
    [Fact] void should_leave_the_legacy_timestamp_unknown() => _legacy.RunningStateChangedAt.ShouldBeNull();

    void Destroy() => _storage.Dispose();
}
