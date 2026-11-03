// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.MongoDB.EventSequences;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Generations.Fencing.for_UnfencedSinkWrites;

[Collection(ReplicaSetMongoDBCollection.Name)]
public class when_a_conditional_upsert_one_condition_holds(ReplicaSetMongoDBFixture fixture) : for_FencedSinkWrites.given(fixture)
{
    Exception _error;
    UpdateResult _result;
    BsonDocument _document;

    async Task Establish() => await _models.InsertOneAsync(new BsonDocument { { "_id", 2 }, { "watermark", 5 } });

    async Task Because()
    {
        _error = await Catch.Exception(async () => _result = await ((ISinkWrites)new UnfencedSinkWrites()).ConditionalUpsertOne(_models, new BsonDocument { { "_id", 2 }, { "watermark", new BsonDocument("$lt", 10) } }, new BsonDocument("_id", 2), new BsonDocument("$set", new BsonDocument("watermark", 10)), new UpdateOptions { IsUpsert = true }));
        _document = await _models.Find(new BsonDocument("_id", 2)).FirstOrDefaultAsync();
    }

    [Fact] void should_not_report_a_failure() => _error.ShouldBeNull();
    [Fact] void should_match_the_document() => _result.MatchedCount.ShouldEqual(1);
    [Fact] void should_leave_the_expected_watermark() => _document["watermark"].AsInt32.ShouldEqual(10);
}
