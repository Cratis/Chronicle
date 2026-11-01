// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Observation;

using KernelObserverState = Cratis.Chronicle.Storage.Observation.ObserverState;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Observers.for_ObserverStateStorage.when_renaming;

public class with_type_set_control : given.an_observer_state_storage
{
    static readonly EventTypeId _type = new("event-a");
    KernelObserverState _renamed;

    async Task Establish()
    {
        var control = new ObserverTypeSetControl(
            new(7),
            new("reactor-subscriber", ObserverFilters.None),
            new Dictionary<EventTypeId, ObserverTypeSetMember> { [_type] = new(new(3), new(42)) },
            [new("connection-one", null, [new(_type, new(1))], EventGenerationDelivery.Compatibility),
             new("connection-two", null, [new(_type, new(2))], EventGenerationDelivery.Pinned)]);
        await _storage.Save(new() { Identifier = "observer", TypeSetControl = control }, Concepts.EventSequences.EventSequenceId.Log);
    }

    async Task Because()
    {
        await _storage.Rename("observer", "renamed");
        _renamed = await _storage.Get("renamed");
    }

    [Fact] void should_keep_the_contract() => _renamed.TypeSetControl.Contract.SubscriberType.ShouldEqual("reactor-subscriber");
    [Fact] void should_keep_the_revision() => _renamed.TypeSetControl.Revision.Value.ShouldEqual(7);
    [Fact] void should_keep_the_members() => _renamed.TypeSetControl.Members[_type].ShouldEqual(new ObserverTypeSetMember(new(3), new(42)));
    [Fact] void should_keep_the_capabilities() => _renamed.TypeSetControl.Registrations.Select(registration => registration.GenerationDelivery).ShouldEqual([EventGenerationDelivery.Compatibility, EventGenerationDelivery.Pinned]);
}
