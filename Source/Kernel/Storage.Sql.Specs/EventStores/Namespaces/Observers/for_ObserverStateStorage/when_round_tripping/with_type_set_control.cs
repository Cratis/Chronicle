// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Observation;
using KernelObserverState = Cratis.Chronicle.Storage.Observation.ObserverState;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Observers.for_ObserverStateStorage.when_round_tripping;

public class with_type_set_control : given.an_observer_state_storage
{
    static readonly EventTypeId _type = new("event-a");
    KernelObserverState _saved;

    async Task Because()
    {
        var control = new ObserverTypeSetControl(
            new(7),
            new("reactor-subscriber", new(["important"], new("customer"), new("orders"))),
            new Dictionary<EventTypeId, ObserverTypeSetMember> { [_type] = new(new(3), new(42)) },
            [new("connection-one", Guid.Parse("11111111-1111-1111-1111-111111111111"), [new(_type, new(1))], EventGenerationDelivery.Compatibility),
             new("connection-two", null, [new(_type, new(2))], EventGenerationDelivery.Pinned)]);
        await _storage.Save(new() { Identifier = "observer", TypeSetControl = control });
        _saved = await _storage.Get("observer");
    }

    [Fact] void should_keep_one_member_for_both_generations() => _saved.TypeSetControl.Members.Count.ShouldEqual(1);
    [Fact] void should_keep_the_membership_revision() => _saved.TypeSetControl.Revision.Value.ShouldEqual(7);
    [Fact] void should_keep_the_member_incarnation() => _saved.TypeSetControl.Members[_type].Incarnation.Value.ShouldEqual(3);
    [Fact] void should_keep_the_member_fence() => _saved.TypeSetControl.Members[_type].JoinFence.Value.ShouldEqual(42UL);
    [Fact] void should_keep_independent_generation_pins() => _saved.TypeSetControl.Registrations.SelectMany(registration => registration.DeclaredTypes).Select(type => type.Generation.Value).ShouldEqual([1U, 2U]);
    [Fact] void should_keep_independent_delivery_modes() => _saved.TypeSetControl.Registrations.Select(registration => registration.GenerationDelivery).ShouldEqual([EventGenerationDelivery.Compatibility, EventGenerationDelivery.Pinned]);
    [Fact] void should_keep_the_contract_filters() => _saved.TypeSetControl.Contract.Filters.Tags.ShouldEqual(["important"]);
    [Fact] void should_keep_the_source_type_filter() => _saved.TypeSetControl.Contract.Filters.EventSourceType.Value.ShouldEqual("customer");
    [Fact] void should_keep_the_stream_type_filter() => _saved.TypeSetControl.Contract.Filters.EventStreamType.Value.ShouldEqual("orders");
    [Fact] void should_keep_registration_ids() => _saved.TypeSetControl.Registrations[0].RegistrationId.ShouldEqual(Guid.Parse("11111111-1111-1111-1111-111111111111"));
}
