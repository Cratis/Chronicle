// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSources;

namespace Cratis.Chronicle.Seeding.for_EventSeeding;

public class when_explicit_routing_contradicts_the_typed_definition : given.a_seeding_builder
{
    Exception _error;

    void Establish() => _eventSources.GetFor(typeof(Order)).Returns(new EventSourceDefinition(typeof(Order), "Order", "", ConcurrencyDimensions.None, [new EventStream("Lines", "", ConcurrencyDimensions.None)]));

    void Because() => _error = Catch.Exception(() => _seeding.ForEvents([new EventForEventSourceId("source", new TestEvent("value"))
    {
        EventSource = typeof(Order),
        EventStream = "Lines",
        EventSourceType = "DifferentSource"
    }]));

    [Fact] void should_refuse_contradictory_routing() => _error.ShouldBeOfExactType<EventRoutingContradictsEventSource>();
    [Fact] void should_not_send_a_seed_request() => _request.ShouldBeNull();

    class Order;
}
