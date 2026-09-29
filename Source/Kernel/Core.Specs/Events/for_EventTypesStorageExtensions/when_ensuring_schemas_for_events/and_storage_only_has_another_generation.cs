// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;

namespace Cratis.Chronicle.Events.for_EventTypesStorageExtensions.when_ensuring_schemas_for_events;

public class and_storage_only_has_another_generation : given.an_event_type_stored_at_two_generations
{
    Dictionary<EventType, EventTypeSchema> _schemas = [];

    void Establish() => _eventTypes.GetFor(Arg.Any<IEnumerable<EventType>>())
        .Returns(Task.FromResult<IEnumerable<EventTypeSchema>>([_second_generation_schema]));

    async Task Because() => await _eventTypes.EnsureSchemasFor(_schemas, [EventStoredAt(first_generation)]);

    [Fact] void should_not_use_the_schema_of_the_other_generation() => _schemas.ShouldBeEmpty();
}
