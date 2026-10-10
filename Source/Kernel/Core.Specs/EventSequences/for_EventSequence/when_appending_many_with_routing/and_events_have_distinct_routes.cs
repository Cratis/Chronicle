// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// Conformance: Screenplay relies on this (Cratis/Chronicle#4658).
namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_many_with_routing;

public class and_events_have_distinct_routes : when_appending_with_routing.given.a_routed_event_sequence
{
    AppendManyResult _result;

    async Task Because() => _result = await new Sequences.AppendManyForEventSources(
        EventStore,
        EventStoreNamespace,
        SequenceId,
        [
            new("account-1", "Account", "Payments", "September", new(_eventType.Id.Value, 1, false), "{}"),
            new("order-2", "Order", "Fulfillment", "shipment-42", new(_eventType.Id.Value, 1, false), "{}")
        ]).Handle(_grainFactory, _causation, _principal);

    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_store_both_events() => _storedRoutes.Length.ShouldEqual(2);
    [Fact] void should_store_the_first_event_source_type() => _storedRoutes[0].SourceType.Value.ShouldEqual("Account");
    [Fact] void should_store_the_first_event_stream_type() => _storedRoutes[0].StreamType.Value.ShouldEqual("Payments");
    [Fact] void should_store_the_first_event_stream_id() => _storedRoutes[0].StreamId.Value.ShouldEqual("September");
    [Fact] void should_store_the_second_event_source_type() => _storedRoutes[1].SourceType.Value.ShouldEqual("Order");
    [Fact] void should_store_the_second_event_stream_type() => _storedRoutes[1].StreamType.Value.ShouldEqual("Fulfillment");
    [Fact] void should_store_the_second_event_stream_id() => _storedRoutes[1].StreamId.Value.ShouldEqual("shipment-42");
}
