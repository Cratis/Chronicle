// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;

namespace Cratis.Chronicle.Events.for_EventTypesStorageExtensions.when_ensuring_schemas_for_events;

public class and_a_batch_holds_only_redacted_events : given.an_event_type_stored_at_two_generations
{
    static readonly EventType _redacted = new(GlobalEventTypes.Redaction, EventTypeGeneration.First);

    Dictionary<EventType, EventTypeSchema> _schemas = [];

    async Task Because()
    {
        await _eventTypes.EnsureSchemasFor(_schemas, [EventStoredAt(_redacted)]);
        await _eventTypes.EnsureSchemasFor(_schemas, [EventStoredAt(_redacted)]);
    }

    [Fact] void should_never_ask_storage() => _eventTypes.DidNotReceive().GetFor(Arg.Any<IEnumerable<EventType>>());
    [Fact] void should_hold_no_schema() => _schemas.Count.ShouldEqual(0);
}
