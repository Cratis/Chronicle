// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Observation;

using KernelObserverState = Cratis.Chronicle.Storage.Observation.ObserverState;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Observers.for_ObserverStateStorage.when_saving.and_control_already_exists;

public class without_control : given.an_observer_state_storage
{
    static readonly EventTypeId _type = new("event-a");
    KernelObserverState _saved;

    async Task Establish()
    {
        var control = new ObserverTypeSetControl(
            new(7),
            new("reactor-subscriber", ObserverFilters.None),
            new Dictionary<EventTypeId, ObserverTypeSetMember> { [_type] = new(new(3), new(42)) },
            [new("connection-one", null, [new(_type, new(2))], EventGenerationDelivery.Pinned)]);
        await _storage.Save(new() { Identifier = "observer", TypeSetControl = control }, Concepts.EventSequences.EventSequenceId.Log);
    }

    async Task Because()
    {
        await _storage.Save(new() { Identifier = "observer", NextEventSequenceNumber = new(123) }, Concepts.EventSequences.EventSequenceId.Log);
        _saved = await _storage.Get("observer");
    }

    [Fact] void should_preserve_the_membership_revision() => _saved.TypeSetControl.Revision.Value.ShouldEqual(7);
    [Fact] void should_preserve_the_contract() => _saved.TypeSetControl.Contract.SubscriberType.ShouldEqual("reactor-subscriber");
    [Fact] void should_preserve_the_member() => _saved.TypeSetControl.Members[_type].ShouldEqual(new ObserverTypeSetMember(new(3), new(42)));
    [Fact] void should_preserve_the_registration_generation() => _saved.TypeSetControl.Registrations.Single().DeclaredTypes.Single().Generation.Value.ShouldEqual(2U);
    [Fact] void should_preserve_the_registration_delivery_mode() => _saved.TypeSetControl.Registrations.Single().GenerationDelivery.ShouldEqual(EventGenerationDelivery.Pinned);
    [Fact] void should_save_the_other_state_changes() => _saved.NextEventSequenceNumber.Value.ShouldEqual(123UL);
}
