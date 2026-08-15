// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Reactors.for_ReactorDelivery.when_comparing;

/// <summary>
/// Namespaces are separate tenants of the same event store, each with its own sequence numbering. One tenant's
/// receipt must never answer for another's.
/// </summary>
public class and_only_the_namespace_differs : given.a_delivery
{
    ReactorDelivery _other;

    void Because() => _other = ReactorDelivery.For(_reactor, ContextFor(EventStore, "other-tenant", _partition, _sequenceNumber));

    [Fact] void should_not_be_the_same_delivery() => _other.ShouldNotEqual(_delivery);
    [Fact] void should_not_carry_the_same_identity() => _other.Id.ShouldNotEqual(_delivery.Id);
}
