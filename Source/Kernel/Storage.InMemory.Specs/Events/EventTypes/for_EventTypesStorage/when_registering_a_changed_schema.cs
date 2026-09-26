// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Storage.InMemory.Events.EventTypes.for_EventTypesStorage;

public class when_registering_a_changed_schema : given.an_event_types_storage
{
    readonly List<IEnumerable<EventTypeSchema>> _observed = [];
    string _stored;

    async Task Establish()
    {
        await _storage.Register(new EventType("some-event", EventTypeGeneration.First), await JsonSchema.FromJsonAsync("""{"type":"object","properties":{"old":{"type":"string"}}}"""));
        _storage.ObserveLatestForAllEventTypes().Subscribe(_observed.Add);
    }

    async Task Because()
    {
        await _storage.Register(new EventType("some-event", EventTypeGeneration.First), await JsonSchema.FromJsonAsync("""{"type":"object","properties":{"new":{"type":"string"}}}"""));
        _stored = (await _storage.GetFor(new EventTypeId("some-event"))).Schema.ToJson();
    }

    [Fact] void should_publish_the_change() => _observed.Count.ShouldEqual(2);
    [Fact] void should_store_the_changed_schema() => _stored.ShouldContain("new");
}
