// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Webhooks.Envelope.for_DeliveryIdentity;

public class when_computing_identities : Specification
{
    string _key;
    string _delivery;

    void Because()
    {
        _key = DeliveryIdentity.Event("store", "namespace", "observer", "sequence", 42).Value;
        _delivery = DeliveryIdentity.Delivery("store", "namespace", "observer", "sequence", DeliveryKind.Events, [new("partition", 42, 43)]).Value;
    }

    [Fact] void should_match_the_event_vector() => _key.ShouldEqual("8c4e79dad7dbbd6fb6364d0feb82b79fcc7875d88f0ce6ff9b0fa551557aa6ac");
    [Fact] void should_match_the_delivery_vector() => _delivery.ShouldEqual("msg_1857c1f54046f387153746427230b7d8a72a20f3db8c8e6da3165bf86806aa3a");
    [Fact] void should_keep_the_event_key_on_retry() => DeliveryIdentity.Event("store", "namespace", "observer", "sequence", 42).Value.ShouldEqual(_key);
    [Fact] void should_keep_the_delivery_id_on_retry() => DeliveryIdentity.Delivery("store", "namespace", "observer", "sequence", DeliveryKind.Events, [new("partition", 42, 43)]).Value.ShouldEqual(_delivery);
    [Fact] void should_scope_the_event_to_the_store() => DeliveryIdentity.Event("other", "namespace", "observer", "sequence", 42).Value.ShouldNotEqual(_key);
    [Fact] void should_scope_the_event_to_the_namespace() => DeliveryIdentity.Event("store", "other", "observer", "sequence", 42).Value.ShouldNotEqual(_key);
    [Fact] void should_scope_the_event_to_the_observer() => DeliveryIdentity.Event("store", "namespace", "other", "sequence", 42).Value.ShouldNotEqual(_key);
    [Fact] void should_scope_the_event_to_the_number() => DeliveryIdentity.Event("store", "namespace", "observer", "sequence", 43).Value.ShouldNotEqual(_key);
    [Fact] void should_not_allow_coordinate_boundary_collisions() => DeliveryIdentity.Event("a", "bc", "observer", "sequence", 42).ShouldNotEqual(DeliveryIdentity.Event("ab", "c", "observer", "sequence", 42));
    [Fact] void should_scope_the_delivery_to_the_namespace() => DeliveryIdentity.Delivery("store", "other", "observer", "sequence", DeliveryKind.Events, [new("partition", 42, 43)]).Value.ShouldNotEqual(_delivery);
    [Fact] void should_scope_the_delivery_to_the_observer() => DeliveryIdentity.Delivery("store", "namespace", "other", "sequence", DeliveryKind.Events, [new("partition", 42, 43)]).Value.ShouldNotEqual(_delivery);
    [Fact] void should_scope_the_delivery_to_the_kind() => DeliveryIdentity.Delivery("store", "namespace", "observer", "sequence", DeliveryKind.Ping, []).Value.ShouldNotEqual(_delivery);
    [Fact] void should_scope_the_event_to_the_sequence() => DeliveryIdentity.Event("store", "namespace", "observer", "other", 42).Value.ShouldNotEqual(_key);
    [Fact] void should_scope_the_delivery_to_the_store() => DeliveryIdentity.Delivery("other", "namespace", "observer", "sequence", DeliveryKind.Events, [new("partition", 42, 43)]).Value.ShouldNotEqual(_delivery);
    [Fact] void should_scope_the_delivery_to_the_sequence() => DeliveryIdentity.Delivery("store", "namespace", "observer", "other", DeliveryKind.Events, [new("partition", 42, 43)]).Value.ShouldNotEqual(_delivery);
    [Fact] void should_scope_the_delivery_to_the_partition() => DeliveryIdentity.Delivery("store", "namespace", "observer", "sequence", DeliveryKind.Events, [new("other", 42, 43)]).Value.ShouldNotEqual(_delivery);
    [Fact] void should_scope_the_delivery_to_the_range() => DeliveryIdentity.Delivery("store", "namespace", "observer", "sequence", DeliveryKind.Events, [new("partition", 42, 44)]).Value.ShouldNotEqual(_delivery);
}
