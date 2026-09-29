// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;

namespace Cratis.Chronicle.Projections.Engine.for_EventTypeSchemasExtensions.when_finding_schema_for_an_event_type;

public class and_the_event_type_has_no_schema : given.event_type_schemas
{
    EventTypeSchema? _result;

    void Because() => _result = new[] { SchemaFor("another-event-type", _secondGeneration) }
        .SchemaFor(new EventType(_eventTypeId, _secondGeneration));

    [Fact] void should_not_return_a_schema() => _result.ShouldBeNull();
}
