// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Storage.MongoDB.Sinks;
using MongoDB.Bson;
using MongoDB.Driver;
using KernelDefinition = Cratis.Chronicle.Concepts.Projections.Definitions.ProjectionDefinition;

namespace Cratis.Chronicle.Storage.MongoDB.Projections.for_ProjectionDefinitionsStorage;

/// <summary>
/// Pins the document a projection definition is stored as. Older and newer kernels read each other's documents, so it must not change.
/// </summary>
/// <param name="fixture">The shared <see cref="MongoDBFixture"/> providing a MongoDB server.</param>
[Collection(MongoDBCollection.Name)]
public class when_saving_a_populated_definition(MongoDBFixture fixture) : for_ProjectionDefinitionConverters.given.a_populated_definition
{
    readonly string _databaseName = MongoDBSpecDatabaseNames.New();
    IMongoClient _client;
    KernelDefinition _restored;
    BsonDocument _document;

    async Task Because()
    {
        _definition = _definition with
        {
            InitialModelState = new JsonObject { ["count"] = 0, ["details"] = new JsonObject { ["names"] = new JsonArray("a", "b") } },
            Tags = ["one", "two"],
            NoAutoMapProperties = ["secret"]
        };
        _client = new MongoClient(fixture.ConnectionString);
        var database = _client.GetDatabase(_databaseName);
        var eventStoreDatabase = Substitute.For<IEventStoreDatabase>();
        eventStoreDatabase.GetCollection<Projection>(Arg.Any<string?>()).Returns(call => database.GetCollection<Projection>(call.Arg<string?>()));
        var storage = new ProjectionDefinitionsStorage(eventStoreDatabase);
        try
        {
            await storage.Save(_definition);
            _restored = await storage.Get(_definition.Identifier);
            _document = await database.GetCollection<BsonDocument>(WellKnownCollectionNames.ProjectionDefinitions).Find(FilterDefinition<BsonDocument>.Empty).SingleAsync();
        }
        finally
        {
            await _client.DropDatabaseAsync(_databaseName);
        }
    }

    [Fact] void should_store_the_documented_document() => JsonNode.DeepEquals(JsonNode.Parse(_document.ToJson()), JsonNode.Parse(StoredDocument.Populated)).ShouldBeTrue();
    [Fact] void should_restore_the_initial_model_state() => _restored.InitialModelState.ToJsonString().ShouldEqual(_definition.InitialModelState.ToJsonString());
    [Fact] void should_restore_the_event_types() => _restored.From.Keys.ShouldContainOnly(_definition.From.Keys.ToArray());
    [Fact] void should_restore_the_children() => _restored.Children.Keys.ShouldContainOnly(_definition.Children.Keys.ToArray());
    [Fact] void should_restore_the_tags() => _restored.Tags.ShouldContainOnly("one", "two");
    [Fact] void should_restore_the_auto_map_exclusions() => _restored.NoAutoMapProperties.Select(_ => _.Path).ShouldContainOnly("secret");
}
