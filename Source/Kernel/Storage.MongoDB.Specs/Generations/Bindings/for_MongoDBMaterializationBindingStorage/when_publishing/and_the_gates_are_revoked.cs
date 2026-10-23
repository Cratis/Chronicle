// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Generations;
using Cratis.Chronicle.Storage.Generations.Bindings;
using Cratis.Chronicle.Storage.MongoDB.EventSequences;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Generations.Bindings.for_MongoDBMaterializationBindingStorage.when_publishing;

[Collection(ReplicaSetMongoDBCollection.Name)]
public class and_the_gates_are_revoked(ReplicaSetMongoDBFixture fixture) : given.a_mongo_binding(fixture)
{
    async Task Establish()
    {
        await Reserve(true);
        await Database.GetCollection<BsonDocument>("fence-gates").InsertManyAsync([
            new BsonDocument { { "_id", new GenerationGateId(_key, MaterializationGeneration.First).Value }, { "epoch", 7L }, { "admitted", 2L } },
            new BsonDocument { { "_id", new GenerationGateId(_key, _candidate).Value }, { "epoch", 9L }, { "admitted", 3L } }]);
    }
    async Task Because()
    {
        await _storage.TryPublish(_store, _key, MaterializationGeneration.First, new(1), _candidate, _receipt);
    }
    [Fact] async Task should_revoke_the_previous_active_once() => (await GateWrites.CurrentEpoch(Database, new(_key, MaterializationGeneration.First))).ShouldEqual(8L);
    [Fact] async Task should_revoke_the_candidate_once() => (await GateWrites.CurrentEpoch(Database, new(_key, _candidate))).ShouldEqual(10L);
    [Fact] async Task should_store_the_publication_fence() => (await _storage.Get(_store, _key))!.Fence.ShouldEqual(new MaterializationFence(new(_key, _candidate), 10L));
    [Fact] async Task should_preserve_admitted() => (await Database.GetCollection<BsonDocument>("fence-gates").Find(new BsonDocument("_id", new GenerationGateId(_key, _candidate).Value)).SingleAsync())["admitted"].AsInt64.ShouldEqual(3L);
}
