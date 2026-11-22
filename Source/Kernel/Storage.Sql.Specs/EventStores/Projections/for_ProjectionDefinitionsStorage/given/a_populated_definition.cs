// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Storage.Sql.for_Database.given;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Projections.for_ProjectionDefinitionsStorage.given;

public class a_populated_definition : a_sqlite_database
{
    protected ProjectionDefinition _definition;
    protected ProjectionDefinitionsStorage _storage;

    async Task Establish()
    {
        var eventType = new EventType("added", 1);
        var mappings = new Dictionary<PropertyPath, string> { ["name"] = "Name" };
        var from = new FromDefinition(mappings, "Key", "Parent");
        var every = new FromEveryDefinition(mappings, true) { Key = "Category", AutoMap = AutoMap.Disabled };
        var child = new ChildrenDefinition(
            "id",
            new Dictionary<EventType, FromDefinition> { [eventType] = from },
            new Dictionary<EventType, JoinDefinition> { [eventType] = new("name", mappings, "Key", AutoMap.Disabled) },
            new Dictionary<PropertyPath, ChildrenDefinition>(),
            every,
            new Dictionary<EventType, RemovedWithDefinition> { [eventType] = new("Key", "Parent") },
            new Dictionary<EventType, RemovedWithJoinDefinition> { [eventType] = new("Key") },
            new(eventType, "Items"),
            AutoMap.Disabled);
        _definition = new(
            ProjectionOwner.Client,
            EventSequenceId.Log,
            "client-projection",
            "client-model",
            true,
            true,
            new JsonObject { ["count"] = 0, ["details"] = new JsonObject { ["names"] = new JsonArray("a", "b") } },
            child.From,
            child.Join,
            new Dictionary<PropertyPath, ChildrenDefinition> { ["items"] = child },
            [new([eventType], from)],
            every,
            child.RemovedWith,
            child.RemovedWithJoin,
            child.FromEventProperty,
            Tags: ["one", "two"],
            Nested: new Dictionary<PropertyPath, ChildrenDefinition> { ["details"] = child },
            NoAutoMapProperties: ["secret"],
            Scope: ProjectionScope.Global);
        _storage = new("projections-store", _database);
        await using var scope = await _database.EventStore("projections-store");
    }
}
