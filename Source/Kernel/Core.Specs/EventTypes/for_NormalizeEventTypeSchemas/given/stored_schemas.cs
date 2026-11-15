// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.EventTypes;
using EventTypesStorage = Cratis.Chronicle.Storage.InMemory.Events.EventTypes.EventTypesStorage;

namespace Cratis.Chronicle.EventTypes.for_NormalizeEventTypeSchemas.given;

public class stored_schemas : Specification
{
    protected EventStoreName _store = "selected-store";
    protected IStorage _storage;
    protected IEventStoreStorage _selected;
    protected IEventTypesCacheClient _cache;
    protected IEventTypesStorage _eventTypes;
    protected IEventTypesStorage _otherStore;
    protected const string Annotated = """{"title":"Useful","description":"Keep","deprecated":false,"properties":{"data":{"type":"string","contentEncoding":"base64"},"literal":{"default":{"deprecated":true}}}}""";

    async Task Establish()
    {
        _storage = Substitute.For<IStorage>();
        _cache = Substitute.For<IEventTypesCacheClient>();
        _eventTypes = new EventTypesStorage();
        _otherStore = new EventTypesStorage();
        _selected = Substitute.For<IEventStoreStorage>();
        _selected.EventTypes.Returns(_eventTypes);
        _storage.GetEventStore(_store).Returns(_selected);
        var other = Substitute.For<IEventStoreStorage>();
        other.EventTypes.Returns(_otherStore);
        _storage.GetEventStore("other-store").Returns(other);
        await _eventTypes.Register(new EventType("affected", 1), await JsonSchema.FromJsonAsync(Annotated));
        await _eventTypes.Register(new EventType("affected", 2), await JsonSchema.FromJsonAsync(Annotated));
        await _eventTypes.Register(new EventType("unselected", 1), await JsonSchema.FromJsonAsync(Annotated));
        await _eventTypes.Register(new EventType("clean", 1), await JsonSchema.FromJsonAsync("""{"title":"Keep"}"""));
        await _otherStore.Register(new EventType("affected", 1), await JsonSchema.FromJsonAsync(Annotated));
    }
}
