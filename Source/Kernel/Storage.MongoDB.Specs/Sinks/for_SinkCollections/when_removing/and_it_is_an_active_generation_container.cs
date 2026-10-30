// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Generations;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.MongoDB.Generations.Bindings;
using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_SinkCollections.when_removing;

[Collection(MongoDBCollection.Name)]
public class and_it_is_an_active_generation_container(MongoDBFixture fixture) : given.two_silos_sharing_a_database(fixture)
{
    Exception _error;
    [Fact] public void should_refuse_removal() => _error.ShouldBeOfExactType<SinkCollectionOperationRefused>();
    [Fact] public async Task should_keep_the_collection() => (await CollectionNames()).ShouldContain(ContainerName);
    [Fact] public async Task should_keep_its_documents() => (await VersionIn(ContainerName)).ShouldEqual("before");
    protected override async Task Establish()
    {
        await Database.GetCollection<BsonDocument>("materialization-bindings").InsertOneAsync(new BsonDocument
        {
            { "_id", "binding" },
            { "generations", new BsonArray { new BsonDocument { { "destination", ContainerName }, { "generation", 1 }, { "state", (int)MaterializationGenerationState.Active } } } }
        });
        var model = new ReadModelDefinition("thing", ContainerName, "Thing", ReadModelOwner.Client, ReadModelSource.Code, ReadModelObserverType.Projection, ReadModelObserverIdentifier.Unspecified, SinkDefinition.None, new Dictionary<ReadModelGeneration, JsonSchema>(), []);
        var sink = new SinkCollections(model, Database, generationClaims: new MongoDBGenerationContainerClaims(Database));
        _error = await Catch.Exception(async () => await sink.Remove(ContainerName));
    }
}
