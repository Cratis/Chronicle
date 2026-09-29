// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;

namespace Cratis.Chronicle.Projections.Engine.for_EventTypeSchemasExtensions.when_finding_schema_for_an_event_type;

public class and_only_another_generation_exists : given.event_type_schemas
{
    EventTypeSchema _secondGenerationSchema;
    EventTypeSchema? _result;

    void Establish() => _secondGenerationSchema = SchemaFor(_eventTypeId, _secondGeneration);

    void Because() => _result = new[] { SchemaFor("another-event-type", EventTypeGeneration.First), _secondGenerationSchema }
        .SchemaFor(new EventType(_eventTypeId, EventTypeGeneration.First));

    [Fact] void should_return_the_schema_stored_for_the_event_type() => _result.ShouldEqual(_secondGenerationSchema);
}
