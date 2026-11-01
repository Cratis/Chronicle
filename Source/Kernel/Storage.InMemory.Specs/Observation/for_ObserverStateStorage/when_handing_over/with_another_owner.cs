// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Storage.InMemory.Observation.for_ObserverStateStorage.when_handing_over;

public class with_another_owner : Specification
{
    ObserverStateStorage _storage;
    ObserverState _original;
    ObserverState _result;
    bool _accepted;

    async Task Establish()
    {
        _storage = new();
        await _storage.Save(new() { Identifier = "observer", NextEventSequenceNumber = new(999) }, "C");
        _original = await _storage.Get("observer");
    }

    async Task Because()
    {
        _accepted = await _storage.HandOver("observer", "A", ObserverSequenceCheckpoint.Fresh("observer", "B"));
        _result = await _storage.Get("observer");
    }

    [Fact] void should_reject_the_handover() => _accepted.ShouldBeFalse();
    [Fact] void should_not_change_the_state() => _result.ShouldEqual(_original);

    void Destroy() => _storage.Dispose();
}
