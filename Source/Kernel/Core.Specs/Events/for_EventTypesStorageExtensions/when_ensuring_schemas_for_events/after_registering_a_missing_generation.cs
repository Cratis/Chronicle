// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;

namespace Cratis.Chronicle.Events.for_EventTypesStorageExtensions.when_ensuring_schemas_for_events;

public class after_registering_a_missing_generation : given.an_event_type_stored_at_two_generations
{
    readonly Dictionary<EventType, EventTypeSchema> _schemas = [];

    async Task Because()
    {
        _eventTypes.GetDefinition(first_generation.Id).Returns(new EventTypeDefinition(first_generation.Id, EventTypeOwner.Client, false, [new(1, _first_generation_schema.Schema)], []));
        await _eventTypes.EnsureSchemasFor(_schemas, [EventStoredAt(second_generation)]);
        _eventTypes.ClearReceivedCalls();

        // Registration replaces the existing cached definition, invalidating its negative generation lookup.
        _eventTypes.GetDefinition(first_generation.Id).Returns(new EventTypeDefinition(first_generation.Id, EventTypeOwner.Client, false, [new(1, _first_generation_schema.Schema), new(2, _second_generation_schema.Schema)], []));
        await _eventTypes.EnsureSchemasFor(_schemas, [EventStoredAt(second_generation)]);
    }

    [Fact] void should_resolve_the_newly_registered_generation() => _schemas[second_generation].ShouldEqual(_second_generation_schema);
    [Fact] void should_load_the_newly_registered_schema() => _eventTypes.Received(1).GetFor(Arg.Is<IEnumerable<EventType>>(types => types.Single() == second_generation));
    [Fact] void should_not_enumerate_storage() => _eventTypes.DidNotReceive().GetAllGenerationsForEventType(Arg.Any<EventType>());
}
