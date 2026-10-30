// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Storage.InMemory.Observation.for_ObserverStateStorage.when_saving.and_control_already_exists;

public class without_control : Specification
{
    ObserverStateStorage _storage;
    ObserverTypeSetControl _control;
    ObserverState _saved;

    async Task Establish()
    {
        _storage = new();
        _control = new(
            new(7),
            new("reactor-subscriber", ObserverFilters.None),
            new Dictionary<EventTypeId, ObserverTypeSetMember> { [new("event-a")] = new(new(3), new(42)) },
            [new("connection-one", null, [new(new("event-a"), new(2))], EventGenerationDelivery.Pinned)]);
        await _storage.Save(new() { Identifier = "observer", TypeSetControl = _control }, Concepts.EventSequences.EventSequenceId.Log);
    }

    async Task Because()
    {
        await _storage.Save(new() { Identifier = "observer", NextEventSequenceNumber = new(123) }, Concepts.EventSequences.EventSequenceId.Log);
        _saved = await _storage.Get("observer");
    }

    [Fact] void should_preserve_the_existing_control() => _saved.TypeSetControl.ShouldEqual(_control);
    [Fact] void should_save_the_other_state_changes() => _saved.NextEventSequenceNumber.Value.ShouldEqual(123UL);

    void Destroy() => _storage.Dispose();
}
