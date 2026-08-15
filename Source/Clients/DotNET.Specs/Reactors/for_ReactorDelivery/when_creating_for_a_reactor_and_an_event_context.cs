// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Reactors.for_ReactorDelivery;

/// <summary>
/// Every component of the identity has to come from what Chronicle already knows about the delivery. Anything
/// invented here - a counter, a timestamp, a new guid - would change between the first delivery and the retry
/// that follows a recovered partition, which is the one thing the identity exists to survive.
/// </summary>
public class when_creating_for_a_reactor_and_an_event_context : given.a_delivery
{
    [Fact] void should_identify_the_reactor() => _delivery.Reactor.ShouldEqual(typeof(OrderConfirmations).GetReactorId());
    [Fact] void should_take_the_event_store_from_the_context() => _delivery.EventStore.ShouldEqual((EventStoreName)EventStore);
    [Fact] void should_take_the_namespace_from_the_context() => _delivery.Namespace.ShouldEqual((EventStoreNamespaceName)Namespace);
    [Fact] void should_take_the_event_sequence_the_reactor_observes() => _delivery.EventSequence.ShouldEqual(EventSequenceId.Log);
    [Fact] void should_take_the_event_source_id_as_the_partition() => _delivery.Partition.ShouldEqual(_partition);
    [Fact] void should_take_the_sequence_number_from_the_context() => _delivery.SequenceNumber.ShouldEqual(_sequenceNumber);
    [Fact] void should_produce_an_identity() => _delivery.Id.Value.ShouldNotBeEmpty();
}
