// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Generations;
using Cratis.Chronicle.Storage.MongoDB.EventSequences;
using Contract = Cratis.Chronicle.Storage.Generations.Bindings.for_MaterializationBindings.given;

namespace Cratis.Chronicle.Storage.MongoDB.Generations.Bindings.for_MongoDBMaterializationBindingStorage;

[Collection(ReplicaSetMongoDBCollection.Name)]
public class when_a_physical_destination_already_exists(ReplicaSetMongoDBFixture fixture) : Contract.a_build_storage<MongoMaterializationBindingStorageHarness>
{
    MongoMaterializationBindingStorageHarness _mongoHarness;
    Exception _error;
    protected override MongoMaterializationBindingStorageHarness CreateHarness() => _mongoHarness = new() { ConnectionString = fixture.ConnectionString };
    async Task Establish() => await _mongoHarness.Database(_store, _key.Namespace).CreateCollectionAsync("models-g2");
    async Task Because() => _error = await Catch.Exception(async () => await Build());
    [Fact] void should_not_adopt_an_unrelated_empty_collection() => _error.ShouldBeOfExactType<GenerationBuildConflict>();
    [Fact] async Task should_not_allocate_a_partial_generation() => (await _storage.Get(_store, _key))!.Next.ShouldEqual(MaterializationGeneration.First);
    [Fact] async Task should_not_persist_execution() => (await _builds.GetExecution(_store, _key, new(2))).ShouldBeNull();
}
