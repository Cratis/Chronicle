// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Sequences.for_EventContextConverters;

public class when_converting_a_stored_context : Specification
{
    Contracts.Sequences.EventContext _contract;

    void Because()
    {
        var stored = Concepts.Events.EventContext.From(
            (EventStoreName)"orders",
            (EventStoreNamespaceName)"acme",
            new Concepts.Events.EventType("OrderPlaced", EventTypeGeneration.First, false),
            EventSourceType.Default,
            new EventSourceId("order-42"),
            EventStreamType.All,
            EventStreamId.Default,
            EventSequenceNumber.First,
            CorrelationId.New());

        _contract = stored.ToApi().ToContract();
    }

    [Fact] void should_carry_the_event_store_the_event_belongs_to() => _contract.EventStore.ShouldEqual("orders");
    [Fact] void should_carry_the_namespace_the_event_belongs_to() => _contract.Namespace.ShouldEqual("acme");
    [Fact] void should_default_the_subject_to_the_event_source() => _contract.Subject.ShouldEqual("order-42");
}
