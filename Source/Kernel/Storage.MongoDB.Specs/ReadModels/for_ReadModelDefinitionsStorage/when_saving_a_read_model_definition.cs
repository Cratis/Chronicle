// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.MongoDB.Projections;
using Cratis.Chronicle.Storage.MongoDB.Sinks;
using MongoDB.Bson;
using MongoDB.Driver;
using KernelDefinition = Cratis.Chronicle.Concepts.ReadModels.ReadModelDefinition;

namespace Cratis.Chronicle.Storage.MongoDB.ReadModels.for_ReadModelDefinitionsStorage;

/// <summary>
/// Pins the document a read model definition is stored as. Older and newer kernels read each other's documents, so it must not change.
/// </summary>
/// <param name="fixture">The shared <see cref="MongoDBFixture"/> providing a MongoDB server.</param>
[Collection(MongoDBCollection.Name)]
public class when_saving_a_read_model_definition(MongoDBFixture fixture) : Specification
{
    readonly string _databaseName = MongoDBSpecDatabaseNames.New();
    KernelDefinition _definition;
    KernelDefinition _restored;
    BsonDocument _document;

    async Task Because()
    {
        _definition = new KernelDefinition(
            "orders",
            "orders-container",
            "Orders",
            ReadModelOwner.Client,
            ReadModelSource.Code,
            ReadModelObserverType.Projection,
            "orders-projection",
            SinkDefinition.None,
            new Dictionary<ReadModelGeneration, JsonSchema>
            {
                [ReadModelGeneration.First] = await JsonSchema.FromJsonAsync("""{"type":"object","properties":{"id":{"type":"string"}}}"""),
                [2] = await JsonSchema.FromJsonAsync("""{"type":"object","properties":{"id":{"type":"string"},"name":{"type":"string"}}}""")
            },
            [new IndexDefinition(new PropertyPath("name"))]);
        var client = new MongoClient(fixture.ConnectionString);
        var database = client.GetDatabase(_databaseName);
        var eventStoreDatabase = Substitute.For<IEventStoreDatabase>();
        eventStoreDatabase.GetCollection<ReadModel>(Arg.Any<string?>()).Returns(call => database.GetCollection<ReadModel>(call.Arg<string?>()));
        var storage = new ReadModelDefinitionsStorage(eventStoreDatabase);
        try
        {
            await storage.Save(_definition);
            _restored = await storage.Get(_definition.Identifier);
            _document = await database.GetCollection<BsonDocument>(WellKnownCollectionNames.ReadModelDefinitions).Find(FilterDefinition<BsonDocument>.Empty).SingleAsync();
        }
        finally
        {
            await client.DropDatabaseAsync(_databaseName);
        }
    }

    [Fact] void should_store_the_documented_document() => JsonNode.DeepEquals(JsonNode.Parse(_document.ToJson()), JsonNode.Parse(StoredDocument.Orders)).ShouldBeTrue();
    [Fact] void should_restore_the_identifier() => _restored.Identifier.ShouldEqual(_definition.Identifier);
    [Fact] void should_restore_the_generations() => _restored.Schemas.Keys.Order().ShouldEqual(_definition.Schemas.Keys.Order());
    [Fact] void should_restore_every_schema() => _definition.Schemas.All(_ => JsonNode.DeepEquals(JsonNode.Parse(_restored.Schemas[_.Key].ToJson()), JsonNode.Parse(_.Value.ToJson()))).ShouldBeTrue();
    [Fact] void should_restore_the_indexes() => _restored.Indexes.ShouldEqual(_definition.Indexes);
}
