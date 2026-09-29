// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;

namespace Cratis.Chronicle.Events.for_EventTypesStorageExtensions.when_ensuring_schemas_for_events;

public class and_several_events_share_a_type_and_generation : given.an_event_type_stored_at_two_generations
{
    Dictionary<EventType, EventTypeSchema> _schemas = [];

    async Task Because() => await _eventTypes.EnsureSchemasFor(
        _schemas,
        [EventStoredAt(first_generation), EventStoredAt(first_generation), EventStoredAt(second_generation)]);

    [Fact] void should_ask_storage_once_for_each_type_and_generation() =>
        _eventTypes.Received(1).GetFor(Arg.Is<IEnumerable<EventType>>(_ => _.Count() == 2));
    [Fact] void should_hold_a_schema_for_each_generation() => _schemas.Count.ShouldEqual(2);
}
