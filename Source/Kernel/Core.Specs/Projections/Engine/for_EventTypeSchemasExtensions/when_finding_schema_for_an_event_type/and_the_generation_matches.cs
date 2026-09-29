// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;

namespace Cratis.Chronicle.Projections.Engine.for_EventTypeSchemasExtensions.when_finding_schema_for_an_event_type;

public class and_the_generation_matches : given.event_type_schemas
{
    EventTypeSchema _firstGenerationSchema;
    EventTypeSchema _secondGenerationSchema;
    EventTypeSchema? _result;

    void Establish()
    {
        _firstGenerationSchema = SchemaFor(_eventTypeId, EventTypeGeneration.First);
        _secondGenerationSchema = SchemaFor(_eventTypeId, _secondGeneration);
    }

    void Because() => _result = new[] { _secondGenerationSchema, _firstGenerationSchema }.SchemaFor(new EventType(_eventTypeId, EventTypeGeneration.First));

    [Fact] void should_return_the_schema_for_the_requested_generation() => _result.ShouldEqual(_firstGenerationSchema);
}
