// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Storage.InMemory.Events.EventTypes.for_EventTypesStorage;

public class when_getting_the_latest_for_all_event_types : given.an_event_types_storage
{
    static readonly EventTypeGeneration _second = EventTypeGeneration.First + 1;

    IEnumerable<EventTypeSchema> _schemas;

    async Task Establish()
    {
        await _storage.Register(new EventType("first-event", EventTypeGeneration.First), new JsonSchema(), EventTypeOwner.Client, EventTypeSource.User);
        await _storage.Register(new EventType("second-event", EventTypeGeneration.First), new JsonSchema());
        await _storage.Register(new EventType("second-event", _second), new JsonSchema());
    }

    async Task Because() => _schemas = await _storage.GetLatestForAllEventTypes();

    [Fact] void should_return_every_registered_event_type() =>
        _schemas.Select(_ => _.Type.Id).ShouldContainOnly(new EventTypeId("first-event"), new EventTypeId("second-event"));

    [Fact] void should_return_the_latest_generation_of_each() =>
        _schemas.Single(_ => _.Type.Id == "second-event").Type.Generation.ShouldEqual(_second);

    [Fact] void should_carry_the_source_it_was_registered_with() =>
        _schemas.Single(_ => _.Type.Id == "first-event").Source.ShouldEqual(EventTypeSource.User);
}
