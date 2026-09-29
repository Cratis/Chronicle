// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;

namespace Cratis.Chronicle.Events.for_EventTypesStorageExtensions.when_ensuring_schemas_for_events;

public class and_the_schemas_are_already_loaded : given.an_event_type_stored_at_two_generations
{
    Dictionary<EventType, EventTypeSchema> _schemas;

    void Establish() => _schemas = new()
    {
        { first_generation, _first_generation_schema },
        { second_generation, _second_generation_schema }
    };

    async Task Because() => await _eventTypes.EnsureSchemasFor(_schemas, [EventStoredAt(first_generation), EventStoredAt(second_generation)]);

    [Fact] void should_not_ask_storage_for_any_schema() => _eventTypes.DidNotReceive().GetFor(Arg.Any<IEnumerable<EventType>>());
}
