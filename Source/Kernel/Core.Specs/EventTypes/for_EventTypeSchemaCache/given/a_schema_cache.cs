// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.EventTypes;

namespace Cratis.Chronicle.EventTypes.for_EventTypeSchemaCache.given;

public class a_schema_cache : Specification
{
    protected static readonly EventStoreName _eventStore = "some-event-store";
    protected static readonly EventTypeId _eventTypeId = "some-event-type";
    protected static readonly EventTypeId _otherEventTypeId = "some-other-event-type";
    protected static readonly EventTypeGeneration _firstGeneration = EventTypeGeneration.First;
    protected static readonly EventTypeGeneration _secondGeneration = new(2);

    protected IEventTypesStorage _eventTypesStorage;
    protected IEventTypeSchemaCache _cache;

    void Establish()
    {
        _eventTypesStorage = Substitute.For<IEventTypesStorage>();
        _eventTypesStorage
            .GetFor(Arg.Any<EventTypeId>(), Arg.Any<EventTypeGeneration>())
            .Returns(callInfo => Task.FromResult(SchemaFor(callInfo.ArgAt<EventTypeId>(0), callInfo.ArgAt<EventTypeGeneration>(1))));

        var eventStoreStorage = Substitute.For<IEventStoreStorage>();
        eventStoreStorage.EventTypes.Returns(_eventTypesStorage);
        var storage = Substitute.For<IStorage>();
        storage.GetEventStore(Arg.Any<EventStoreName>()).Returns(eventStoreStorage);

        _cache = new EventTypeSchemaCache(storage);
    }

    protected int SchemaLookups() =>
        _eventTypesStorage
            .ReceivedCalls()
            .Count(call => call.GetMethodInfo().Name == nameof(IEventTypesStorage.GetFor));

    static EventTypeSchema SchemaFor(EventTypeId eventTypeId, EventTypeGeneration generation) =>
        new(
            new EventType(eventTypeId, generation),
            EventTypeOwner.Client,
            EventTypeSource.Code,
            JsonSchema.FromJson("{\"type\":\"object\"}"));
}
