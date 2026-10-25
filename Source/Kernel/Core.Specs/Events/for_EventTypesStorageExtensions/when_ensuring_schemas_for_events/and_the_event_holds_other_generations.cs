// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Events.for_EventTypesStorageExtensions.when_ensuring_schemas_for_events;

public class and_the_event_holds_other_generations : given.an_event_type_stored_at_two_generations
{
    Dictionary<EventType, EventTypeSchema> _schemas = [];
    EventType[] _requested;
    AppendedEvent _event;

    void Establish()
    {
        _event = EventStoredAt(new(first_generation.Id, 4)) with
        {
            OriginalContent = "{}",
            GenerationalContent = new Dictionary<int, string> { [2] = "{}" },
            Revisions = [new(3, CorrelationId.NotSet, [], Identity.System, DateTimeOffset.UtcNow, "{}")]
        };
        _eventTypes.GetFor(Arg.Any<IEnumerable<EventType>>()).Returns(call =>
        {
            _requested = call.Arg<IEnumerable<EventType>>().ToArray();
            return Task.FromResult<IEnumerable<EventTypeSchema>>(_requested.Select(type => new EventTypeSchema(type, EventTypeOwner.Client, EventTypeSource.Code, new JsonSchema())).ToArray());
        });
    }

    async Task Because() => await _eventTypes.EnsureSchemasFor(_schemas, [_event, _event]);

    [Fact] void should_load_current_original_generational_and_revision_schemas() => _schemas.Keys.Select(type => type.Generation.Value).Order().ShouldEqual([1U, 2U, 3U, 4U]);
    [Fact] void should_request_each_generation_only_once() => _requested.Length.ShouldEqual(4);
}
