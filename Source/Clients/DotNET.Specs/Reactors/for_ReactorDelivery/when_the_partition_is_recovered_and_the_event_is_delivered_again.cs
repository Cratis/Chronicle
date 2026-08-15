// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Reactors.for_ReactorDelivery;

/// <summary>
/// Recovering a failed partition re-reads the same partition from the sequence number it failed on and hands the
/// events to the same observer as an ordinary observation - a fresh reactor instance in a fresh scope, and a
/// context rebuilt from storage. The identity has to come out the same across all of that, or a consumer keyed
/// on it would charge the card twice.
/// </summary>
public class when_the_partition_is_recovered_and_the_event_is_delivered_again : given.a_delivery
{
    ReactorDelivery _redelivery;

    void Because() => _redelivery = ReactorDelivery.For(
        new OrderConfirmations(),
        ContextFor(EventStore, Namespace, _partition, _sequenceNumber));

    [Fact] void should_be_the_same_delivery() => _redelivery.ShouldEqual(_delivery);
    [Fact] void should_carry_the_same_identity() => _redelivery.Id.ShouldEqual(_delivery.Id);
}
