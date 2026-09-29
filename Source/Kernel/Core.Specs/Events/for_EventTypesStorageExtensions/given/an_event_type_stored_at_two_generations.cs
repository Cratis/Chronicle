// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.EventTypes;

namespace Cratis.Chronicle.Events.for_EventTypesStorageExtensions.given;

public class an_event_type_stored_at_two_generations : Specification
{
    protected static readonly EventType first_generation = new("some-event", EventTypeGeneration.First);
    protected static readonly EventType second_generation = new("some-event", new EventTypeGeneration(2));

    protected IEventTypesStorage _eventTypes;
    protected EventTypeSchema _first_generation_schema;
    protected EventTypeSchema _second_generation_schema;

    void Establish()
    {
        _first_generation_schema = new(first_generation, EventTypeOwner.Client, EventTypeSource.Code, new JsonSchema { Description = "first" });
        _second_generation_schema = new(second_generation, EventTypeOwner.Client, EventTypeSource.Code, new JsonSchema { Description = "second" });

        _eventTypes = Substitute.For<IEventTypesStorage>();
        _eventTypes.GetFor(Arg.Any<IEnumerable<EventType>>())
            .Returns(callInfo => Task.FromResult<IEnumerable<EventTypeSchema>>(
                [.. callInfo.Arg<IEnumerable<EventType>>().Select(SchemaFor)]));
    }

    protected static AppendedEvent EventStoredAt(EventType eventType) => AppendedEvent.EmptyWithEventType(eventType);

    EventTypeSchema SchemaFor(EventType eventType) =>
        eventType.Generation == first_generation.Generation ? _first_generation_schema : _second_generation_schema;
}
