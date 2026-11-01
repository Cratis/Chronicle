// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Storage.InMemory.Observation.for_ObserverSequenceCheckpointsStorage;

public class when_removing_all_for_an_observer : Specification
{
    ObserverSequenceCheckpointsStorage _storage;
    ObserverSequenceCheckpoint _first;
    ObserverSequenceCheckpoint _second;
    ObserverSequenceCheckpoint _other;

    async Task Establish()
    {
        _storage = new();
        await _storage.Save(ObserverSequenceCheckpoint.Fresh("observer", "A"));
        await _storage.Save(ObserverSequenceCheckpoint.Fresh("observer", "B"));
        await _storage.Save(ObserverSequenceCheckpoint.Fresh("other", "A"));
    }

    async Task Because()
    {
        await _storage.RemoveAllFor("observer");
        await _storage.RemoveAllFor("observer");
        _first = await _storage.Get("observer", "A");
        _second = await _storage.Get("observer", "B");
        _other = await _storage.Get("other", "A");
    }

    [Fact] void should_remove_the_first_sequence() => _first.ShouldBeNull();
    [Fact] void should_remove_the_second_sequence() => _second.ShouldBeNull();
    [Fact] void should_keep_other_observers() => _other.ShouldNotBeNull();
}
