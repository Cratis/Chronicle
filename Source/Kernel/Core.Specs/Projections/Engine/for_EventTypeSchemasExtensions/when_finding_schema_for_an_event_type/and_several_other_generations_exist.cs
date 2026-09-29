// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;

namespace Cratis.Chronicle.Projections.Engine.for_EventTypeSchemasExtensions.when_finding_schema_for_an_event_type;

public class and_several_other_generations_exist : given.event_type_schemas
{
    static readonly EventTypeGeneration _thirdGeneration = new(3U);

    EventTypeSchema _thirdGenerationSchema;
    EventTypeSchema? _result;

    void Establish() => _thirdGenerationSchema = SchemaFor(_eventTypeId, _thirdGeneration);

    void Because() => _result = new[] { SchemaFor(_eventTypeId, _secondGeneration), _thirdGenerationSchema }
        .SchemaFor(new EventType(_eventTypeId, EventTypeGeneration.First));

    [Fact] void should_return_the_schema_for_the_latest_generation() => _result.ShouldEqual(_thirdGenerationSchema);
}
