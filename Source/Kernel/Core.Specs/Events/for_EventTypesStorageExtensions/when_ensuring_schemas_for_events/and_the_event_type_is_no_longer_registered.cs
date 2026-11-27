// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Storage.EventTypes;

namespace Cratis.Chronicle.Events.for_EventTypesStorageExtensions.when_ensuring_schemas_for_events;

public class and_the_event_type_is_no_longer_registered : given.an_event_type_stored_at_two_generations
{
    Dictionary<EventType, EventTypeSchema> _schemas = [];
    EventType _retired = new("a-retired-event", EventTypeGeneration.First);

    void Establish() => _eventTypes.GetDefinition(_retired.Id)
        .Returns<Task<EventTypeDefinition>>(_ => throw new MissingEventSchemaForEventType("store", _retired.Id, _retired.Generation));

    async Task Because() => await _eventTypes.EnsureSchemasFor(_schemas, [EventStoredAt(_retired)]);

    [Fact] void should_not_fail() => _schemas.ShouldNotBeNull();
    [Fact] void should_not_hold_a_schema_for_the_retired_type() => _schemas.ContainsKey(_retired).ShouldBeFalse();
}
