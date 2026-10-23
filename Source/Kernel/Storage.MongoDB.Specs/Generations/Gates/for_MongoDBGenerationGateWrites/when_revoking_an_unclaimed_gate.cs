// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Generations.Bindings;
using Cratis.Chronicle.Storage.MongoDB.EventSequences;
using Cratis.Chronicle.Storage.MongoDB.Generations.Bindings.for_MongoDBMaterializationBindingStorage.given;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Generations.Gates.for_MongoDBGenerationGateWrites;

[Collection(ReplicaSetMongoDBCollection.Name)]
public class when_revoking_an_unclaimed_gate(ReplicaSetMongoDBFixture fixture) : a_mongo_binding(fixture)
{
    long _epoch;
    GenerationGateId Gate => new(_key, new(23));
    async Task Because()
    {
        using var session = await Database.Client.StartSessionAsync();
        session.StartTransaction(new(ReadConcern.Snapshot, ReadPreference.Primary, WriteConcern.WMajority));
        _epoch = await GateWrites.Revoke(Database, session, Gate);
        await session.CommitTransactionAsync();
    }
    [Fact] void should_start_at_one() => _epoch.ShouldEqual(1L);
    [Fact] async Task should_be_majority_visible() => (await GateWrites.CurrentEpoch(Database, Gate)).ShouldEqual(1L);
    [Fact] async Task should_use_the_b1_document_shape()
    {
        var document = await Database.GetCollection<BsonDocument>("fence-gates").Find(new BsonDocument("_id", Gate.Value)).SingleAsync();
        document["epoch"].BsonType.ShouldEqual(BsonType.Int64);
        document["admitted"].AsInt64.ShouldEqual(0L);
    }
}
