// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Storage.Sql.EventStores.EventTypes.for_EventTypesStorage.given;

public class migration_versions : an_event_types_storage
{
    protected EventTypeDefinition _definition;
    protected EventTypeMigrationsVersion _version;
    protected EventTypeId _id;

    async Task Establish()
    {
        _id = "versioned-event";
        _definition = new(_id, EventTypeOwner.Client, false, [new(1, new JsonSchema()), new(2, new JsonSchema())], [Migration("source")]);
        await _storage.Register(_definition);
        _version = EventTypeMigrationsVersion.For(_definition.Migrations);
        await _storage.RecordMigrationsVersion(_id, _version, _definition.Migrations);
    }

    protected static EventTypeMigrationDefinition Migration(string expression) => new(1, 2, [], new JsonObject { ["value"] = expression }, new JsonObject());
}
