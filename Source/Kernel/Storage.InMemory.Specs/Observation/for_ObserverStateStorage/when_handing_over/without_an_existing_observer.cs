// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Storage.InMemory.Observation.for_ObserverStateStorage.when_handing_over;

public class without_an_existing_observer : Specification
{
    ObserverStateStorage _storage;
    bool _accepted;
    IEnumerable<ObserverState> _states;

    void Establish() => _storage = new();

    async Task Because()
    {
        _accepted = await _storage.HandOver("observer", null, ObserverSequenceCheckpoint.Fresh("observer", "B"));
        _states = await _storage.GetAll();
    }

    [Fact] void should_not_accept_a_missing_observer() => _accepted.ShouldBeFalse();
    [Fact] void should_not_create_an_observer() => _states.ShouldBeEmpty();

    void Destroy() => _storage.Dispose();
}
