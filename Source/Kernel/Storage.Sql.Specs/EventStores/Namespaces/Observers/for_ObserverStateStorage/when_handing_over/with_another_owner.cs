// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Storage.Observation;
using KernelState = Cratis.Chronicle.Storage.Observation.ObserverState;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Observers.for_ObserverStateStorage.when_handing_over;

public class with_another_owner : given.an_observer_state_storage
{
    KernelState _result;
    bool _accepted;

    async Task Establish() => await _storage.Save(new() { Identifier = "observer", NextEventSequenceNumber = new(999) }, "C");

    async Task Because()
    {
        _accepted = await _storage.HandOver("observer", "A", ObserverSequenceCheckpoint.Fresh("observer", "B"));
        _result = await _storage.Get("observer");
    }

    [Fact] void should_reject_the_handover() => _accepted.ShouldBeFalse();
    [Fact] void should_keep_the_owner() => _result.Owner.ShouldEqual(new EventSequenceId("C"));
    [Fact] void should_keep_the_position() => _result.NextEventSequenceNumber.Value.ShouldEqual(999UL);
}
