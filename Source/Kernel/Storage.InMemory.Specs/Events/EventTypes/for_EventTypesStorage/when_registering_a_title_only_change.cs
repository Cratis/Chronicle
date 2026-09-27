// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Storage.InMemory.Events.EventTypes.for_EventTypesStorage;

public class when_registering_a_title_only_change : given.an_event_types_storage
{
    const string StoredSchema = """{"type":"object","properties":{"key":{"type":"object","title":"OldKey","properties":{"id":{"type":"string"}}}}}""";
    const string IncomingSchema = """{"type":"object","properties":{"key":{"type":"object","title":"NewKey","properties":{"id":{"type":"string"}}}}}""";
    readonly List<IEnumerable<EventTypeSchema>> _observed = [];
    string _stored;

    async Task Establish()
    {
        await _storage.Register(new EventType("some-event", EventTypeGeneration.First), await JsonSchema.FromJsonAsync(StoredSchema));
        _storage.ObserveLatestForAllEventTypes().Subscribe(_observed.Add);
    }

    async Task Because()
    {
        await _storage.Register(new EventType("some-event", EventTypeGeneration.First), await JsonSchema.FromJsonAsync(IncomingSchema));
        _stored = (await _storage.GetFor(new EventTypeId("some-event"))).Schema.ToJson();
    }

    [Fact] void should_not_publish_another_change() => _observed.Count.ShouldEqual(1);
    [Fact] void should_keep_the_stored_schema() => _stored.ShouldContain("OldKey");
}
