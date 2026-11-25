// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;

namespace Cratis.Chronicle.Events.for_EventTypesStorageExtensions.when_ensuring_schemas_for_events;

public class and_registered_schema_storage_fails : given.an_event_type_stored_at_two_generations
{
    readonly Exception _expected = new InvalidOperationException("Schema storage is unavailable");
    readonly Dictionary<EventType, EventTypeSchema> _schemas = [];
    Exception? _actual;

    void Establish() => _eventTypes.GetFor(Arg.Any<IEnumerable<EventType>>()).Returns(_ => Task.FromException<IEnumerable<EventTypeSchema>>(_expected));

    async Task Because()
    {
        try
        {
            await _eventTypes.EnsureSchemasFor(_schemas, [AppendedEvent.EmptyWithEventType(first_generation)]);
        }
        catch (Exception exception)
        {
            _actual = exception;
        }
    }

    [Fact] void should_propagate_the_storage_failure() => _actual.ShouldBeSame(_expected);
    [Fact] void should_not_treat_storage_failure_as_a_missing_generation() => _schemas.ShouldBeEmpty();
}
