// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Reactors.for_ReactorDelivery.when_comparing;

/// <summary>
/// Partitions fail and recover independently, so the partition has to be part of what a delivery is - two
/// partitions sharing one identity would let one partition's recovery suppress the other's work.
/// </summary>
public class and_only_the_partition_differs : given.a_delivery
{
    ReactorDelivery _other;

    void Because() => _other = ReactorDelivery.For(_reactor, ContextFor(EventStore, Namespace, "order-43", _sequenceNumber));

    [Fact] void should_not_be_the_same_delivery() => _other.ShouldNotEqual(_delivery);
    [Fact] void should_not_carry_the_same_identity() => _other.Id.ShouldNotEqual(_delivery.Id);
}
