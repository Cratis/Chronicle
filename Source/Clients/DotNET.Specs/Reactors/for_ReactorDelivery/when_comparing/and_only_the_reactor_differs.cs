// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Reactors.for_ReactorDelivery.when_comparing;

/// <summary>
/// Two reactors observing the same event each get their own delivery of it. Sharing an identity would let the
/// first one to run mark the event done for the other, which never ran at all.
/// </summary>
public class and_only_the_reactor_differs : given.a_delivery
{
    ReactorDelivery _other;

    void Because() => _other = ReactorDelivery.For(new ShipmentNotifications(), ContextFor(EventStore, Namespace, _partition, _sequenceNumber));

    [Fact] void should_not_be_the_same_delivery() => _other.ShouldNotEqual(_delivery);
    [Fact] void should_not_carry_the_same_identity() => _other.Id.ShouldNotEqual(_delivery.Id);
}
