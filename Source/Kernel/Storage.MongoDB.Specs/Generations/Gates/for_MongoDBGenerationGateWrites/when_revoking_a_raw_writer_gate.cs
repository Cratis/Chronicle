// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.MongoDB.EventSequences;
using Cratis.Chronicle.Storage.MongoDB.Generations.Bindings.for_MongoDBMaterializationBindingStorage.given;
using Cratis.Chronicle.Storage.MongoDB.Generations.Fencing;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Generations.Gates.for_MongoDBGenerationGateWrites;

[Collection(ReplicaSetMongoDBCollection.Name)]
public class when_revoking_a_raw_writer_gate(ReplicaSetMongoDBFixture fixture) : a_mongo_binding(fixture)
{
    readonly FenceToken _token = new("store/namespace/read-model", ulong.MaxValue, 0);
    long _epoch;
    BsonDocument _document;

    async Task Because()
    {
        using var session = await Database.Client.StartSessionAsync();
        session.StartTransaction(new(ReadConcern.Snapshot, ReadPreference.Primary, WriteConcern.WMajority));
        _epoch = await GateWrites.Revoke(Database, session, _token.GateId);
        await session.CommitTransactionAsync();
        _document = await Database.GetCollection<BsonDocument>(FenceCollections.Gates).Find(new BsonDocument("_id", _token.GateId)).SingleAsync();
    }

    [Fact] void should_start_at_one() => _epoch.ShouldEqual(1L);
    [Fact] void should_store_the_writer_gate_identity() => _document["_id"].AsString.ShouldEqual(_token.GateId);
    [Fact] void should_preserve_the_epoch_type() => _document[FenceCollections.Epoch].BsonType.ShouldEqual(BsonType.Int64);
    [Fact] void should_preserve_the_writer_document_shape() => _document.Contains(FenceCollections.Admitted).ShouldBeFalse();
    [Fact] async Task should_be_majority_visible_to_the_writer() =>
        (await new FenceGates(Database).CurrentEpoch(_token.MaterializationKey, _token.Generation)).ShouldEqual(1L);
}
