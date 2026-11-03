// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.MongoDB.EventSequences;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Generations.Fencing.for_UnfencedSinkWrites;

[Collection(ReplicaSetMongoDBCollection.Name)]
public class when_a_rechecked_bulk_upsert_is_followed_by_a_failing_request(ReplicaSetMongoDBFixture fixture) : for_FencedSinkWrites.given(fixture)
{
    Exception _error;
    WriteModel<BsonDocument> _rejected;

    async Task Establish() => await _models.InsertOneAsync(new BsonDocument { { "_id", 2 }, { "watermark", 20 } });

    async Task Because()
    {
        var rejected = new UpdateOneModel<BsonDocument>(new BsonDocument { { "_id", 2 }, { "watermark", new BsonDocument("$lt", 10) } }, new BsonDocument("$set", new BsonDocument("watermark", 10))) { IsUpsert = true };
        ConditionalUpsertRequests.Mark(rejected);
        _rejected = rejected;
        _error = await Catch.Exception(() => new UnfencedSinkWrites().BulkWrite(_models, [Insert(1), rejected, Insert(1)], CancellationToken.None));
    }

    [Fact] void should_report_the_failed_request() => _error.ShouldBeOfExactType<MongoBulkWriteException<BsonDocument>>();
    [Fact] void should_report_the_rechecked_request_as_the_first_processed_request_so_the_sink_can_realign() => ReferenceEquals(((MongoBulkWriteException<BsonDocument>)_error).Result.ProcessedRequests[0], _rejected).ShouldBeTrue();
}
