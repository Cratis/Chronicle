// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Projections.Engine.for_EventTypeSchemasExtensions.when_finding_schema_for_an_event_type.given;

public class event_type_schemas : Specification
{
    protected static readonly EventTypeId _eventTypeId = "the-event-type";
    protected static readonly EventTypeGeneration _secondGeneration = new(2U);

    protected static EventTypeSchema SchemaFor(EventTypeId id, EventTypeGeneration generation) =>
        new(new EventType(id, generation), EventTypeOwner.Client, EventTypeSource.Code, new JsonSchema());
}
