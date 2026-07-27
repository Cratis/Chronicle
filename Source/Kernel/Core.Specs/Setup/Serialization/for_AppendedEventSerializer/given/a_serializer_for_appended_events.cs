// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.EventTypes;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.EventTypes;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Serialization;

namespace Cratis.Chronicle.Setup.Serialization.for_AppendedEventSerializer.given;

public class a_serializer_for_appended_events : Specification
{
    protected static readonly EventStoreName _eventStore = "some-event-store";
    protected static readonly EventTypeId _eventTypeId = "some-event-type";

    protected IEventTypesStorage _eventTypesStorage;
    protected IEventTypeSchemaCache _schemaCache;
    protected EventTypeSchema _schema;
    protected Serializer _serializer;

    void Establish()
    {
        _schema = new EventTypeSchema(
            new EventType(_eventTypeId, EventTypeGeneration.First),
            EventTypeOwner.Client,
            EventTypeSource.Code,
            JsonSchema.FromJson("{\"type\":\"object\"}"));

        _eventTypesStorage = Substitute.For<IEventTypesStorage>();
        var eventStoreStorage = Substitute.For<IEventStoreStorage>();
        eventStoreStorage.EventTypes.Returns(_eventTypesStorage);
        var storage = Substitute.For<IStorage>();
        storage.GetEventStore(Arg.Any<EventStoreName>()).Returns(eventStoreStorage);

        var services = new ServiceCollection();
        services.AddSingleton(new JsonSerializerOptions());
        services.AddSingleton(Substitute.For<IExpandoObjectConverter>());
        services.AddSingleton(storage);
        services.AddSerializer(builder => builder.Services.AddCustomSerializers());
        var provider = services.BuildServiceProvider();
        _schemaCache = provider.GetRequiredService<IEventTypeSchemaCache>();
        _serializer = provider.GetRequiredService<Serializer>();
    }

    protected static AppendedEvent AnEvent() =>
        new(
            EventContext.Empty with
            {
                EventType = new EventType(_eventTypeId, EventTypeGeneration.First),
                EventStore = _eventStore
            },
            new ExpandoObject());

    protected byte[] Serialize(AppendedEvent appendedEvent) => _serializer.SerializeToArray(appendedEvent);

    protected void SchemaLookupReturns() =>
        _eventTypesStorage
            .GetFor(Arg.Any<EventTypeId>(), Arg.Any<EventTypeGeneration>())
            .Returns(_ => Task.FromResult(_schema));

    protected int SchemaLookups() =>
        _eventTypesStorage
            .ReceivedCalls()
            .Count(call => call.GetMethodInfo().Name == nameof(IEventTypesStorage.GetFor));
}
