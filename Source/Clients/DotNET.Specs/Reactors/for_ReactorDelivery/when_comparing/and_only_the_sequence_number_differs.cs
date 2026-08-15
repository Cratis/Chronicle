// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Reactors.for_ReactorDelivery.when_comparing;

/// <summary>
/// The next event in the same partition is a genuinely distinct delivery, not a repeat of the previous one.
/// </summary>
public class and_only_the_sequence_number_differs : given.a_delivery
{
    ReactorDelivery _other;

    void Because() => _other = ReactorDelivery.For(_reactor, ContextFor(EventStore, Namespace, _partition, _sequenceNumber + 1));

    [Fact] void should_not_be_the_same_delivery() => _other.ShouldNotEqual(_delivery);
    [Fact] void should_not_carry_the_same_identity() => _other.Id.ShouldNotEqual(_delivery.Id);
}
