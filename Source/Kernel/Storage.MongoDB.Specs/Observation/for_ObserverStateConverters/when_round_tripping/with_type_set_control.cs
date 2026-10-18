// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Observation;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using KernelObserverState = Cratis.Chronicle.Storage.Observation.ObserverState;

namespace Cratis.Chronicle.Storage.MongoDB.Observation.for_ObserverStateConverters.when_round_tripping;

public class with_type_set_control : Specification
{
    static readonly EventTypeId _type = new("event-a");
    KernelObserverState _ordinary;
    KernelObserverState _joined;

    void Because()
    {
        var control = new ObserverTypeSetControl(
            new(7),
            new("reactor-subscriber", new(["important"], new("customer"), new("orders"))),
            new Dictionary<EventTypeId, ObserverTypeSetMember> { [_type] = new(new(3), new(42)) },
            [new("connection-one", Guid.Parse("11111111-1111-1111-1111-111111111111"), [new(_type, new(1))], EventGenerationDelivery.Compatibility),
             new("connection-two", null, [new(_type, new(2))], EventGenerationDelivery.Pinned)]);
        var state = new KernelObserverState { Identifier = "observer", TypeSetControl = control };
        var bson = state.ToMongoDB().ToBson();
        _ordinary = BsonSerializer.Deserialize<ObserverState>(bson).ToKernel();
        _joined = BsonSerializer.Deserialize<ObserverStateWithFailedPartitions>(bson).ToKernel();
    }

    [Fact] void should_keep_one_member_for_both_generations() => _ordinary.TypeSetControl.Members.Count.ShouldEqual(1);
    [Fact] void should_keep_the_membership_revision() => _ordinary.TypeSetControl.Revision.Value.ShouldEqual(7);
    [Fact] void should_keep_the_member_incarnation() => _ordinary.TypeSetControl.Members[_type].Incarnation.Value.ShouldEqual(3);
    [Fact] void should_keep_the_member_fence() => _ordinary.TypeSetControl.Members[_type].JoinFence.Value.ShouldEqual(42UL);
    [Fact] void should_keep_independent_generation_pins() => _ordinary.TypeSetControl.Registrations.SelectMany(registration => registration.DeclaredTypes).Select(type => type.Generation.Value).ShouldEqual([1U, 2U]);
    [Fact] void should_keep_independent_delivery_modes() => _ordinary.TypeSetControl.Registrations.Select(registration => registration.GenerationDelivery).ShouldEqual([EventGenerationDelivery.Compatibility, EventGenerationDelivery.Pinned]);
    [Fact] void should_keep_the_contract() => _ordinary.TypeSetControl.Contract.Filters.Tags.ShouldEqual(["important"]);
    [Fact] void should_keep_the_subscriber_type() => _ordinary.TypeSetControl.Contract.SubscriberType.ShouldEqual("reactor-subscriber");
    [Fact] void should_keep_connection_identities() => _ordinary.TypeSetControl.Registrations.Select(registration => registration.ConnectionId.Value).ShouldEqual(["connection-one", "connection-two"]);
    [Fact] void should_keep_registration_ids() => _ordinary.TypeSetControl.Registrations[0].RegistrationId.ShouldEqual(Guid.Parse("11111111-1111-1111-1111-111111111111"));
    [Fact] void should_keep_control_on_the_joined_read() => _joined.TypeSetControl.Revision.Value.ShouldEqual(7);
    [Fact] void should_keep_the_joined_member_fence() => _joined.TypeSetControl.Members[_type].JoinFence.Value.ShouldEqual(42UL);
    [Fact] void should_keep_the_joined_generation_pins() => _joined.TypeSetControl.Registrations.SelectMany(registration => registration.DeclaredTypes).Select(type => type.Generation.Value).ShouldEqual([1U, 2U]);
}
