// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Reactors.for_ReactorDelivery;

/// <summary>
/// The identity is what a consumer writes into its own storage and reads back on the next delivery, possibly in
/// another process and possibly years later. That makes the exact rendering a compatibility surface: changing it
/// silently invalidates every receipt anyone has stored, and every side effect they cover runs a second time.
/// Pinning it here means that change cannot happen by accident.
/// </summary>
public class when_reading_the_identity_as_a_key : given.a_delivery
{
    [Fact] void should_render_every_component_in_order() =>
        _delivery.Id.Value.ShouldEqual($"{typeof(OrderConfirmations).FullName}#orders#default#event-log#order-42#7");

    [Fact] void should_render_the_same_value_for_an_equal_delivery() =>
        ReactorDelivery.For(new OrderConfirmations(), ContextFor(EventStore, Namespace, _partition, _sequenceNumber)).Id.ShouldEqual(_delivery.Id);
}
