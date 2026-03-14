// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.EventTypes;

namespace Cratis.Chronicle.EventSequences.Migrations.for_EventTypeMigrations.given;

public class all_dependencies : Specification
{
    protected IStorage _storage;
    protected IEventStoreStorage _eventStoreStorage;
    protected IEventTypesStorage _eventTypesStorage;
    protected IExpandoObjectConverter _expandoObjectConverter;
    protected EventTypeMigrations _eventTypeMigrations;
    protected EventStoreName _eventStoreName;
    protected EventType _eventType;
    protected JsonObject _content;

    void Establish()
    {
        _storage = Substitute.For<IStorage>();
        _eventStoreStorage = Substitute.For<IEventStoreStorage>();
        _eventTypesStorage = Substitute.For<IEventTypesStorage>();
        _expandoObjectConverter = Substitute.For<IExpandoObjectConverter>();
        _eventStoreName = "test-store";
        _storage.GetEventStore(Arg.Any<EventStoreName>()).Returns(_eventStoreStorage);
        _eventStoreStorage.EventTypes.Returns(_eventTypesStorage);
        _eventTypeMigrations = new EventTypeMigrations(_storage, _expandoObjectConverter);
        _eventType = new EventType(Guid.NewGuid().ToString(), 1);
        _content = new JsonObject { ["name"] = "John Doe" };
    }
}
