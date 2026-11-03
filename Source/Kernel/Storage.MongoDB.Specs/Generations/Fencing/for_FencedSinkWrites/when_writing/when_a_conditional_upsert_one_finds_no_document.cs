// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.MongoDB.EventSequences;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Generations.Fencing.for_FencedSinkWrites.when_writing;

[Collection(ReplicaSetMongoDBCollection.Name)]
public class when_a_conditional_upsert_one_finds_no_document(ReplicaSetMongoDBFixture fixture) : given(fixture)
{
    Exception _error;
    UpdateResult _result;
    BsonDocument _document;

    async Task Because()
    {
        _error = await Catch.Exception(async () => _result = await _writes.ConditionalUpsertOne(_models, new BsonDocument { { "_id", 2 }, { "watermark", new BsonDocument("$lt", 10) } }, new BsonDocument("_id", 2), new BsonDocument("$set", new BsonDocument("watermark", 10)), new UpdateOptions { IsUpsert = true }));
        _document = await _models.Find(new BsonDocument("_id", 2)).FirstOrDefaultAsync();
    }

    [Fact] void should_not_report_a_failure() => _error.ShouldBeNull();
    [Fact] void should_create_the_document() => _document.ShouldNotBeNull();
    [Fact] void should_leave_the_expected_watermark() => _document["watermark"].AsInt32.ShouldEqual(10);
}
