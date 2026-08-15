// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Reactors.for_ReactorDelivery.when_comparing;

/// <summary>
/// A reactor adorned for another event store observes that store's inbox rather than the local event log, and the
/// two sequences number their events independently. The identity follows the sequence the reactor actually
/// observes, so the same sequence number in each is two different deliveries.
/// </summary>
public class and_the_reactor_observes_another_event_stores_inbox : given.a_delivery
{
    ReactorDelivery _other;

    void Because() => _other = ReactorDelivery.For(new UpstreamOrderConfirmations(), ContextFor(EventStore, Namespace, _partition, _sequenceNumber));

    [Fact] void should_observe_the_inbox_of_the_source_event_store() =>
        _other.EventSequence.ShouldEqual(new EventSequenceId($"{EventSequenceId.InboxPrefix}{UpstreamOrderConfirmations.SourceEventStore}"));

    [Fact] void should_not_be_the_same_delivery() => _other.ShouldNotEqual(_delivery);
    [Fact] void should_not_carry_the_same_identity() => _other.Id.ShouldNotEqual(_delivery.Id);
}
