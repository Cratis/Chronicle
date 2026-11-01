// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Storage.InMemory.Observation.for_ObserverStateStorage.when_handing_over;

public class with_no_owner : Specification
{
    ObserverStateStorage _storage;
    ObserverState _result;
    bool _accepted;

    async Task Establish()
    {
        _storage = new();
        await _storage.Save(new() { Identifier = "observer", NextEventSequenceNumber = new(999) }, null!);
    }

    async Task Because()
    {
        _accepted = await _storage.HandOver("observer", null, ObserverSequenceCheckpoint.Fresh("observer", "B"));
        _result = await _storage.Get("observer");
    }

    [Fact] void should_accept_the_legacy_owner() => _accepted.ShouldBeTrue();
    [Fact] void should_set_the_target_owner() => _result.Owner.ShouldEqual(new EventSequenceId("B"));
    [Fact] void should_start_at_the_first_event() => _result.NextEventSequenceNumber.ShouldEqual(EventSequenceNumber.First);

    void Destroy() => _storage.Dispose();
}
