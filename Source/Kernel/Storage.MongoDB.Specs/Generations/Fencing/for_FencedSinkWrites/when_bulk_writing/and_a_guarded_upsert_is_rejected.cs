// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.MongoDB.EventSequences;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Generations.Fencing.for_FencedSinkWrites.when_bulk_writing;

[Collection(ReplicaSetMongoDBCollection.Name)]
public class and_a_guarded_upsert_is_rejected(ReplicaSetMongoDBFixture fixture) : given(fixture)
{
    Exception _error;
    long _count;
    BsonDocument _last;

    async Task Establish() => await _models.InsertOneAsync(new BsonDocument { { "_id", 2 }, { "watermark", 20 } });

    async Task Because()
    {
        var rejected = new UpdateOneModel<BsonDocument>(new BsonDocument { { "_id", 2 }, { "watermark", new BsonDocument("$lt", 10) } }, new BsonDocument("$set", new BsonDocument("watermark", 10))) { IsUpsert = true };
        WriteModel<BsonDocument>[] operations = [Insert(1), rejected, Insert(3), new UpdateOneModel<BsonDocument>(new BsonDocument("_id", 3), new BsonDocument("$set", new BsonDocument("afterInsert", true)))];
        _error = await Catch.Exception(() => _writes.BulkWrite(_models, operations, CancellationToken.None, new HashSet<WriteModel<BsonDocument>> { rejected }));
        _count = await Count();
        _last = await _models.Find(new BsonDocument("_id", 3)).SingleAsync();
    }

    [Fact] void should_not_report_a_failure() => _error.ShouldBeNull();
    [Fact] void should_apply_the_prefix_and_suffix() => _count.ShouldEqual(3);
    [Fact] void should_preserve_order_in_the_suffix() => _last["afterInsert"].AsBoolean.ShouldBeTrue();
}
