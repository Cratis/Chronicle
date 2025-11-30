// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Storage.EventTypes;

namespace Cratis.Chronicle.Grains.EventSequences.Migrations.for_EventTypeMigrations.given;

public class all_dependencies : Specification
{
    protected IEventTypesStorage _eventTypesStorage;
    protected IExpandoObjectConverter _expandoObjectConverter;
    protected EventTypeMigrations _eventTypeMigrations;
    protected EventType _eventType;
    protected JsonObject _content;

    void Establish()
    {
        _eventTypesStorage = Substitute.For<IEventTypesStorage>();
        _expandoObjectConverter = Substitute.For<IExpandoObjectConverter>();
        _eventTypeMigrations = new EventTypeMigrations(_eventTypesStorage, _expandoObjectConverter);
        _eventType = new EventType(Guid.NewGuid().ToString(), 1);
        _content = new JsonObject { ["name"] = "John Doe" };
    }
}
