// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.MongoDB.EventSequences;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Generations.Fencing.for_FencedSinkWrites.when_writing;

[Collection(ReplicaSetMongoDBCollection.Name)]
public class when_a_conditional_bulk_upsert_is_rechecked(ReplicaSetMongoDBFixture fixture) : given(fixture)
{
    Exception _error;
    long _count;
    WriteModel<BsonDocument> _rejected;
    BsonDocument _suppressed;
    BsonDocument _last;

    async Task Establish() => await _models.InsertOneAsync(new BsonDocument { { "_id", 2 }, { "watermark", 20 } });

    async Task Because()
    {
        var rejected = new UpdateOneModel<BsonDocument>(new BsonDocument { { "_id", 2 }, { "watermark", new BsonDocument("$lt", 10) } }, new BsonDocument("$set", new BsonDocument("watermark", 10))) { IsUpsert = true };
        ConditionalUpsertRequests.Mark(rejected);
        _rejected = rejected;
        WriteModel<BsonDocument>[] operations = [Insert(1), rejected, Insert(3), new UpdateOneModel<BsonDocument>(new BsonDocument("_id", 3), new BsonDocument("$set", new BsonDocument("afterInsert", true)))];
        _error = await Catch.Exception(() => _writes.BulkWrite(_models, operations, CancellationToken.None));
        _count = await Count();
        _suppressed = await _models.Find(new BsonDocument("_id", 2)).SingleAsync();
        _last = await _models.Find(new BsonDocument("_id", 3)).SingleAsync();
    }

    [Fact] void should_not_report_a_failure() => _error.ShouldBeNull();
    [Fact] void should_apply_the_prefix_and_suffix() => _count.ShouldEqual(3);
    [Fact] void should_preserve_order_in_the_suffix() => _last["afterInsert"].AsBoolean.ShouldBeTrue();
    [Fact] void should_not_change_the_document_whose_condition_does_not_hold() => _suppressed["watermark"].AsInt32.ShouldEqual(20);
    [Fact] void should_have_rechecked_the_request_as_a_plain_update() => ConditionalUpsertRequests.IsConditionalUpsert(_rejected).ShouldBeFalse();
}
