// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;

namespace Cratis.Chronicle.Events.for_EventTypesStorageExtensions.when_ensuring_schemas_for_events;

public class with_a_warm_definition : given.an_event_type_stored_at_two_generations
{
    readonly Dictionary<EventType, EventTypeSchema> _schemas = [];
    readonly EventType _absent = new(first_generation.Id, 3);
    int _definitionEnumerations;

    void Establish() => _eventTypes.GetDefinition(first_generation.Id).Returns(new EventTypeDefinition(
        first_generation.Id,
        EventTypeOwner.Client,
        false,
        RegisteredGenerations(),
        []));

    async Task Because()
    {
        await _eventTypes.EnsureSchemasFor(_schemas, [EventStoredAt(first_generation), EventStoredAt(_absent)]);
        await _eventTypes.EnsureSchemasFor(_schemas, [EventStoredAt(first_generation), EventStoredAt(_absent)]);
    }

    [Fact] void should_cache_the_absent_generation_lookup() => _definitionEnumerations.ShouldEqual(2);
    [Fact] void should_not_enumerate_storage() => _eventTypes.DidNotReceive().GetAllGenerationsForEventType(Arg.Any<EventType>());
    [Fact] void should_not_query_an_absent_schema() => _eventTypes.DidNotReceive().GetFor(Arg.Is<IEnumerable<EventType>>(types => types.Contains(_absent)));
    [Fact] void should_only_load_the_registered_schema_once() => _eventTypes.Received(1).GetFor(Arg.Any<IEnumerable<EventType>>());
    [Fact] void should_leave_the_absent_generation_unresolved() => _schemas.ContainsKey(_absent).ShouldBeFalse();

    IEnumerable<EventTypeGenerationDefinition> RegisteredGenerations()
    {
        _definitionEnumerations++;
        yield return new(first_generation.Generation, _first_generation_schema.Schema);
        yield return new(second_generation.Generation, _second_generation_schema.Schema);
    }
}
