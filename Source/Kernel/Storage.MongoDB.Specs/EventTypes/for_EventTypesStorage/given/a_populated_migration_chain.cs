// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.EventSequences.Migrations;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.MongoDB.Sinks;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using EventTypesStorage = Cratis.Chronicle.Storage.MongoDB.Events.EventTypes.EventTypesStorage;
using JsonExpandoObjectConverter = Cratis.Chronicle.Json.ExpandoObjectConverter;

namespace Cratis.Chronicle.Storage.MongoDB.EventTypes.for_EventTypesStorage.given;

public class a_populated_migration_chain(MongoDBFixture fixture) : an_event_types_storage(fixture)
{
    protected EventTypeDefinition _chain;
    protected EventTypeDefinition _stale;
    protected EventTypeDefinition _warm;
    protected EventTypeMigrationsVersion _version;
    protected BsonDocument _before;

    async Task Establish()
    {
        _chain = new(
            _eventTypeId,
            EventTypeOwner.Client,
            false,
            [new(1, Schema("one")), new(2, Schema("two")), new(3, Schema("three"))],
            [
                new(1, 2, [], new JsonObject { ["two"] = "one" }, new JsonObject { ["one"] = "two" }),
                new(2, 3, [], new JsonObject { ["three"] = "two" }, new JsonObject { ["two"] = "three" })
            ]);
        _stale = _chain with { Generations = _chain.Generations.Take(2).ToArray(), Migrations = _chain.Migrations.Take(1).ToArray() };
        await _storage.Register(_chain);
        _version = EventTypeMigrationsVersion.For(_chain.Migrations);
        await _storage.RecordMigrationsVersion(_chain.Id, _version, _chain.Migrations);
        _before = await _storedDocuments.Find(FilterDefinition<BsonDocument>.Empty).SingleAsync();
        _warm = await _storage.GetDefinition(_chain.Id);
        await _storage.GetFor(_chain.Id, 3);
    }

    protected Task<EventTypeDefinition> ReadCold() => new EventTypesStorage(EventStoreName.NotSet, _database, Substitute.For<ILogger<EventTypesStorage>>()).GetDefinition(_chain.Id);

    protected Task<IEnumerable<EventTypeId>> RegisterBatch(EventTypeDefinition definition) =>
        _storage.Register([new EventTypeToRegister(definition, EventTypeSource.Code)]);

    protected void ShouldRetainSchemas(EventTypeDefinition actual) => actual.Generations.OrderBy(_ => _.Generation.Value).Select(_ => _.Schema.ToJson()).SequenceEqual(_chain.Generations.Select(_ => _.Schema.ToJson())).ShouldBeTrue();

    protected void ShouldRetainBridges(EventTypeDefinition actual) => EventTypeMigrationsVersion.For(actual.Migrations).ShouldEqual(_version);

    protected static async Task ShouldMigrateAcrossChain(EventTypeDefinition definition, (uint Generation, string Property) from, (uint Generation, string Property) to)
    {
        var converter = new JsonExpandoObjectConverter(new TypeFormats());
        var engine = new EventTypeMigrations(Substitute.For<IStorage>(), converter);
        var content = new JsonObject { [from.Property] = "payload" };
        var schema = definition.Generations.Single(_ => _.Generation.Value == from.Generation).Schema;
        var result = await engine.MigrateToAllGenerations(definition, new(definition.Id, from.Generation), content, converter.ToExpandoObject(content, schema));
        result.Count.ShouldEqual(3);
        ((IDictionary<string, object?>)result[(EventTypeGeneration)to.Generation])[to.Property].ShouldEqual("payload");
    }

    protected static EventTypeMigrationDefinition Bridge(uint from, uint to, string up, string down) => new(from, to, [], new JsonObject { ["value"] = up }, new JsonObject { ["value"] = down });

    static JsonSchema Schema(string marker)
    {
        var schema = JsonSchema.FromJson($$$$"""{"type":"object","description":"{{{{marker}}}}","properties":{"{{{{marker}}}}":{"type":"string"}}}""");
        schema.EnsureComplianceMetadata();
        return schema;
    }
}
