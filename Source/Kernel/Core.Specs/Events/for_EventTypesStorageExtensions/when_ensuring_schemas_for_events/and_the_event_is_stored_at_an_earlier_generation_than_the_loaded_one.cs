// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;

namespace Cratis.Chronicle.Events.for_EventTypesStorageExtensions.when_ensuring_schemas_for_events;

public class and_the_event_is_stored_at_an_earlier_generation_than_the_loaded_one : given.an_event_type_stored_at_two_generations
{
    Dictionary<EventType, EventTypeSchema> _schemas;

    void Establish() => _schemas = new() { { second_generation, _second_generation_schema } };

    async Task Because() => await _eventTypes.EnsureSchemasFor(_schemas, [EventStoredAt(first_generation)]);

    [Fact] void should_load_the_schema_of_the_generation_the_event_was_stored_at() => _schemas[first_generation].ShouldEqual(_first_generation_schema);
    [Fact] void should_keep_the_schema_that_was_already_loaded() => _schemas[second_generation].ShouldEqual(_second_generation_schema);
}
