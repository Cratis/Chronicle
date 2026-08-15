// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Reactors.for_ReactorDelivery;

/// <summary>
/// A replay re-delivers the same event to the same reactor, so it is the same delivery repeated and gets the same
/// identity. The observation state deliberately stays out of the identity: it says why the event arrived again,
/// not which delivery it is, and folding it in would let a receipt written on the live path miss on replay.
/// </summary>
public class when_the_event_is_replayed : given.a_delivery
{
    ReactorDelivery _replayed;

    void Because() => _replayed = ReactorDelivery.For(
        _reactor,
        ContextFor(EventStore, Namespace, _partition, _sequenceNumber) with { ObservationState = EventObservationState.Replay });

    [Fact] void should_be_the_same_delivery() => _replayed.ShouldEqual(_delivery);
    [Fact] void should_carry_the_same_identity() => _replayed.Id.ShouldEqual(_delivery.Id);
}
