// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Observation.Reactors;

namespace Cratis.Chronicle.Observation.Reactors.for_ReactorDefinitionIdentity.given;

public class a_stored_and_an_incoming_reactor_definition : Specification
{
    protected ReactorDefinition _stored;
    protected ReactorDefinition _incoming;
    protected bool _result;

    void Establish()
    {
        _stored = CreateDefinition();
        _incoming = CreateDefinition();
    }

    static ReactorDefinition CreateDefinition() => new(
        "the-reactor",
        ReactorOwner.Client,
        EventSequenceId.Log,
        [
            new(new EventType("first-event-type", EventTypeGeneration.First), WellKnownExpressions.EventSourceId),
            new(new EventType("second-event-type", EventTypeGeneration.First), WellKnownExpressions.EventSourceId)
        ],
        true,
        ["first-tag", "second-tag"],
        new ObserverFilters(["filter-tag"], new EventSourceType("source-type"), new EventStreamType("stream-type")))
    {
        GenerationDelivery = EventGenerationDelivery.Compatibility,
        StartAtTail = false
    };
}
