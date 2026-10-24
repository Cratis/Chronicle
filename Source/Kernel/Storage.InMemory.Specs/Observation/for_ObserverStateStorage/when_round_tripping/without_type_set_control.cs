// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Storage.InMemory.Observation.for_ObserverStateStorage.when_round_tripping;

public class without_type_set_control : Specification
{
    ObserverStateStorage _storage;
    ObserverState _saved;
    ObserverState _missing;

    void Establish() => _storage = new();

    async Task Because()
    {
        await _storage.Save(new() { Identifier = "legacy-observer" }, Concepts.EventSequences.EventSequenceId.Log);
        _saved = await _storage.Get("legacy-observer");
        _missing = await _storage.Get("missing-observer");
    }

    [Fact] void should_keep_the_saved_legacy_observer_uninitialized() => _saved.TypeSetControl.ShouldBeNull();
    [Fact] void should_keep_the_missing_observer_uninitialized() => _missing.TypeSetControl.ShouldBeNull();

    void Destroy() => _storage.Dispose();
}
