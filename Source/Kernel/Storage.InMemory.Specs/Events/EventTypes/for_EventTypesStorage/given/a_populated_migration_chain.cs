// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.EventSequences.Migrations;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Storage.InMemory.Events.EventTypes.for_EventTypesStorage.given;

public class a_populated_migration_chain : an_event_types_storage
{
    protected EventTypeDefinition _chain;
    protected EventTypeDefinition _stale;
    protected EventTypeMigrationsVersion _version;

    async Task Establish()
    {
        _chain = new(
            "additive-chain",
            EventTypeOwner.Client,
            false,
            [new(1, Schema("one")), new(2, Schema("two")), new(3, Schema("three"))],
            [
                new(1, 2, [], new JsonObject { ["two"] = "one" }, new JsonObject { ["one"] = "two" }),
                new(2, 3, [], new JsonObject { ["three"] = "two" }, new JsonObject { ["two"] = "three" })
            ]);
        _stale = _chain with { Generations = _chain.Generations.Take(2).ToArray(), Migrations = _chain.Migrations.Take(1).ToArray() };
        await RegisterBatch(_chain);
        _version = EventTypeMigrationsVersion.For(_chain.Migrations);
        await _storage.RecordMigrationsVersion(_chain.Id, _version, _chain.Migrations);
    }

    protected Task<IEnumerable<EventTypeId>> RegisterBatch(EventTypeDefinition definition) =>
        _storage.Register([new EventTypeToRegister(definition, EventTypeSource.Code)]);

    static JsonSchema Schema(string marker)
    {
        var schema = JsonSchema.FromJson($$$$"""{"type":"object","description":"{{{{marker}}}}","properties":{"{{{{marker}}}}":{"type":"string"}}}""");
        schema.EnsureComplianceMetadata();
        return schema;
    }

    protected static async Task ShouldMigrateAcrossChain(EventTypeDefinition definition, (uint Generation, string Property) from, (uint Generation, string Property) to)
    {
        var converter = new ExpandoObjectConverter(new TypeFormats());
        var engine = new EventTypeMigrations(Substitute.For<IStorage>(), converter);
        var content = new JsonObject { [from.Property] = "payload" };
        var schema = definition.Generations.Single(_ => _.Generation.Value == from.Generation).Schema;
        var result = await engine.MigrateToAllGenerations(definition, new(definition.Id, from.Generation), content, converter.ToExpandoObject(content, schema));
        result.Count.ShouldEqual(3);
        ((IDictionary<string, object?>)result[(EventTypeGeneration)to.Generation])[to.Property].ShouldEqual("payload");
    }

    protected static EventTypeMigrationDefinition Bridge(uint from, uint to, string up, string down) => new(from, to, [], new JsonObject { ["value"] = up }, new JsonObject { ["value"] = down });
}
