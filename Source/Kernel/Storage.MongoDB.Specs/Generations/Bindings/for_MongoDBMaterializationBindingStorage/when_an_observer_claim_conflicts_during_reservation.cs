// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Generations;
using Cratis.Chronicle.Storage.MongoDB.EventSequences;
using MongoDB.Bson;
using MongoDB.Driver;
using Contract = Cratis.Chronicle.Storage.Generations.Bindings.for_MaterializationBindings.given;

namespace Cratis.Chronicle.Storage.MongoDB.Generations.Bindings.for_MongoDBMaterializationBindingStorage;

[Collection(ReplicaSetMongoDBCollection.Name)]
public class when_an_observer_claim_conflicts_during_reservation(ReplicaSetMongoDBFixture fixture) : Contract.a_build_storage<MongoMaterializationBindingStorageHarness>
{
    MongoMaterializationBindingStorageHarness _mongoHarness;
    Exception _error;
    protected override MongoMaterializationBindingStorageHarness CreateHarness() => _mongoHarness = new() { ConnectionString = fixture.ConnectionString };
    async Task Establish()
    {
        var identity = MaterializationGenerationIdentity.For(_key.Projection, new(2), "models");
        await _mongoHarness.Database(_store, _key.Namespace).GetCollection<BsonDocument>(MaterializationBindingCollections.Observers).InsertOneAsync(MaterializationBindingDocuments.Observer(_key, identity, _execution));
    }
    async Task Because() => _error = await Catch.Exception(async () => await Build());
    [Fact] void should_refuse_the_claim_conflict() => _error.ShouldBeOfExactType<GenerationBuildConflict>();
    [Fact] async Task should_roll_back_the_generation_counter() => (await _storage.Get(_store, _key))!.Next.ShouldEqual(MaterializationGeneration.First);
    [Fact] async Task should_roll_back_the_execution() => (await _builds.GetExecution(_store, _key, new(2))).ShouldBeNull();
    [Fact] async Task should_not_record_a_successful_operation() => (await _mongoHarness.Database(_store, _key.Namespace).GetCollection<BsonDocument>(MaterializationBindingCollections.Operations).CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty)).ShouldEqual(0L);
}
