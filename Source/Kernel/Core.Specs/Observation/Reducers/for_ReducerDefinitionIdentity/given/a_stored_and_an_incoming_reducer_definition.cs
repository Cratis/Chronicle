// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Observation.Reducers;

namespace Cratis.Chronicle.Observation.Reducers.for_ReducerDefinitionIdentity.given;

public class a_stored_and_an_incoming_reducer_definition : Specification
{
    protected ReducerDefinition _stored;
    protected ReducerDefinition _incoming;
    protected bool _result;

    void Establish()
    {
        _stored = CreateDefinition();
        _incoming = CreateDefinition();
    }

    static ReducerDefinition CreateDefinition() => new(
        "the-reducer",
        EventSequenceId.Log,
        [
            new(new EventType("first-event-type", EventTypeGeneration.First), WellKnownExpressions.EventSourceId),
            new(new EventType("second-event-type", EventTypeGeneration.First), WellKnownExpressions.EventSourceId)
        ],
        "the-read-model",
        true,
        ["first-tag", "second-tag"],
        new ObserverFilters(["filter-tag"], new EventSourceType("source-type"), new EventStreamType("stream-type")),
        "the-hash")
    {
        GenerationDelivery = EventGenerationDelivery.Compatibility
    };
}
