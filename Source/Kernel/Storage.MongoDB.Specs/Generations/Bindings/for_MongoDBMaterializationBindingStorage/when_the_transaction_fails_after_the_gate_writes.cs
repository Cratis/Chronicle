// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Generations;
using Cratis.Chronicle.Storage.Generations.Bindings;
using Cratis.Chronicle.Storage.MongoDB.EventSequences;
using Cratis.Chronicle.Storage.MongoDB.Generations.Gates;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Generations.Bindings.for_MongoDBMaterializationBindingStorage;

[Collection(ReplicaSetMongoDBCollection.Name)]
public class when_the_transaction_fails_after_the_gate_writes(ReplicaSetMongoDBFixture fixture) : given.a_mongo_binding(fixture)
{
    MaterializationBinding _before;
    Exception _error;
    protected override IGenerationGateWrites GateWrites { get; } = Substitute.For<IGenerationGateWrites>();
    async Task Establish()
    {
        await Reserve(true);
        _before = (await _storage.Get(_store, _key))!;
        var real = new MongoDBGenerationGateWrites();
        var calls = 0;
        GateWrites.Revoke(Arg.Any<IMongoDatabase>(), Arg.Any<IClientSessionHandle>(), Arg.Any<GenerationGateId>(), Arg.Any<CancellationToken>()).Returns(async call =>
        {
            var epoch = await real.Revoke(call.ArgAt<IMongoDatabase>(0), call.ArgAt<IClientSessionHandle>(1), call.ArgAt<GenerationGateId>(2));
            if (++calls == 2) throw new InvalidOperationException("injected after gate writes");
            return epoch;
        });
    }
    async Task Because() => _error = await Catch.Exception(async () => await _storage.TryPublish(_store, _key, MaterializationGeneration.First, new(1), _candidate, _receipt));
    [Fact] void should_surface_the_failure() => _error.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] async Task should_not_change_the_binding()
    {
        var current = (await _storage.Get(_store, _key))!;
        current.Active.ShouldEqual(_before.Active);
        current.Epoch.ShouldEqual(_before.Epoch);
        current.Fence.ShouldBeNull();
        current.Generations.ToArray().ShouldEqual(_before.Generations.ToArray());
    }
    [Fact] async Task should_not_commit_either_gate() => (await Database.GetCollection<BsonDocument>("fence-gates").CountDocumentsAsync(Builders<BsonDocument>.Filter.Empty)).ShouldEqual(0L);
    [Fact] async Task should_not_commit_a_receipt() => (await Database.GetCollection<BsonDocument>("materialization-publication-receipts").CountDocumentsAsync(Builders<BsonDocument>.Filter.Empty)).ShouldEqual(0L);
}
