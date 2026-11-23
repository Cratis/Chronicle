// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Alerts;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Observation.Webhooks;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.EventTypes;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.EventTypes.for_EventTypes.given;

public class an_event_types_with_multiple_stores : Specification
{
    protected EventTypes _eventTypes;
    protected ITypes _types;
    protected IStorage _storage;
    protected Type[] _discovered;
    protected readonly EventStoreName _storeA = "store-a";
    protected readonly EventStoreName _storeB = "store-b";
    protected readonly ConcurrentDictionary<Type, int> _preparations = new();
    protected readonly Dictionary<EventStoreName, IEventTypesStorage> _stores = new();
    protected readonly ConcurrentDictionary<EventStoreName, ConcurrentQueue<JsonSchema>> _schemas = new();
    protected Func<EventStoreName, JsonSchema, Task<bool>> _register = (_, _) => Task.FromResult(true);

    void Establish()
    {
        _discovered = [typeof(AlertRaised), typeof(WebhookAdded), typeof(object)];
        _types = Substitute.For<ITypes>();
        _types.All.Returns(_ => _discovered);
        _storage = Substitute.For<IStorage>();
        AddStore(EventStoreName.System);
        AddStore(_storeA);
        AddStore(_storeB);
        _eventTypes = new(_types, _storage, NullLogger<EventTypes>.Instance, PrepareSchema);
    }

    protected void AddStore(EventStoreName name)
    {
        var store = Substitute.For<IEventTypesStorage>();
        _stores.Add(name, store);
        _schemas[name] = new();
        _storage.GetEventStore(name).EventTypes.Returns(store);
        store.Register(Arg.Any<EventType>(), Arg.Any<JsonSchema>(), EventTypeOwner.Server, EventTypeSource.Code)
            .Returns(call =>
            {
                var schema = call.Arg<JsonSchema>();
                _schemas[name].Enqueue(schema);
                return _register(name, schema);
            });
    }

    protected JsonSchema CreateSchema() => new(JsonNode.Parse("""
        {
            "type": "object",
            "properties": {
                "details": {
                    "type": "object",
                    "properties": { "name": { "type": "string" } },
                    "required": ["name"]
                }
            },
            "required": ["details"]
        }
        """)!.AsObject());

    protected async Task ShouldRegister(EventStoreName store, Type type, int count = 1) =>
        await _stores[store].Received(count).Register(type.GetEventType(), Arg.Any<JsonSchema>(), EventTypeOwner.Server, EventTypeSource.Code);

    JsonSchema PrepareSchema(Type type)
    {
        _preparations.AddOrUpdate(type, 1, (_, count) => count + 1);
        return CreateSchema();
    }
}
