// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Storage.InMemory.Observation.for_ObserverStateStorage.when_round_tripping;

public class with_type_set_control : Specification
{
    static readonly EventTypeId _type = new("event-a");
    ObserverStateStorage _storage;
    ObserverState _saved;
    ObserverState _renamed;

    void Establish() => _storage = new();

    async Task Because()
    {
        var control = new ObserverTypeSetControl(
            new(7),
            new("reactor-subscriber", new(["important"], new("customer"), new("orders"))),
            new Dictionary<EventTypeId, ObserverTypeSetMember> { [_type] = new(new(3), new(42)) },
            [new("connection-one", Guid.Parse("11111111-1111-1111-1111-111111111111"), [new(_type, new(1))], EventGenerationDelivery.Compatibility),
             new("connection-two", null, [new(_type, new(2))], EventGenerationDelivery.Pinned)]);
        await _storage.Save(new() { Identifier = "observer", TypeSetControl = control }, Concepts.EventSequences.EventSequenceId.Log);
        _saved = await _storage.Get("observer");
        await _storage.Rename("observer", "renamed");
        _renamed = await _storage.Get("renamed");
    }

    [Fact] void should_keep_one_member_for_both_generations() => _saved.TypeSetControl.Members.Count.ShouldEqual(1);
    [Fact] void should_keep_the_membership_revision() => _saved.TypeSetControl.Revision.Value.ShouldEqual(7);
    [Fact] void should_keep_the_member_incarnation() => _saved.TypeSetControl.Members[_type].Incarnation.Value.ShouldEqual(3);
    [Fact] void should_keep_the_member_fence() => _saved.TypeSetControl.Members[_type].JoinFence.Value.ShouldEqual(42UL);
    [Fact] void should_keep_independent_generation_pins() => _saved.TypeSetControl.Registrations.SelectMany(registration => registration.DeclaredTypes).Select(type => type.Generation.Value).ShouldEqual([1U, 2U]);
    [Fact] void should_keep_independent_delivery_modes() => _saved.TypeSetControl.Registrations.Select(registration => registration.GenerationDelivery).ShouldEqual([EventGenerationDelivery.Compatibility, EventGenerationDelivery.Pinned]);
    [Fact] void should_keep_the_contract_on_rename() => _renamed.TypeSetControl.Contract.SubscriberType.ShouldEqual("reactor-subscriber");
    [Fact] void should_keep_the_members_on_rename() => _renamed.TypeSetControl.Members[_type].JoinFence.Value.ShouldEqual(42UL);
    [Fact] void should_keep_registration_ids() => _saved.TypeSetControl.Registrations[0].RegistrationId.ShouldEqual(Guid.Parse("11111111-1111-1111-1111-111111111111"));
    [Fact] void should_not_invent_retirement_results() => _saved.TypeSetControl.RetirementResults.ShouldBeNull();
    [Fact] void should_not_invent_a_pending_change() => _saved.TypeSetControl.PendingChange.ShouldBeNull();
    [Fact] void should_not_invent_retirement_results_on_rename() => _renamed.TypeSetControl.RetirementResults.ShouldBeNull();
    [Fact] void should_not_invent_a_pending_change_on_rename() => _renamed.TypeSetControl.PendingChange.ShouldBeNull();
    [Fact] void should_not_put_generation_pins_on_members() => typeof(ObserverTypeSetMember).GetProperties().Select(property => property.Name).Order().ShouldEqual(["Incarnation", "JoinFence"]);

    void Destroy() => _storage.Dispose();
}
