// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Reactors.for_ReactorDelivery.when_comparing;

/// <summary>
/// One client process can register the same reactor against several event stores, and each numbers its events
/// from zero. Without the event store in the identity, the two would collide on every sequence number.
/// </summary>
public class and_only_the_event_store_differs : given.a_delivery
{
    ReactorDelivery _other;

    void Because() => _other = ReactorDelivery.For(_reactor, ContextFor("archive", Namespace, _partition, _sequenceNumber));

    [Fact] void should_not_be_the_same_delivery() => _other.ShouldNotEqual(_delivery);
    [Fact] void should_not_carry_the_same_identity() => _other.Id.ShouldNotEqual(_delivery.Id);
}
