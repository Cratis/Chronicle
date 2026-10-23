// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.MongoDB.EventSequences;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Generations.Fencing.for_FencedSinkWrites;

[Collection(ReplicaSetMongoDBCollection.Name)]
public class when_flushing_more_operations_than_the_cap(ReplicaSetMongoDBFixture fixture) : given(fixture)
{
    long _count;
    long _receipts;

    async Task Because()
    {
        using var writes = new FencedSinkWrites(new FixedFenceTokenSource(_token), _executor, 2);
        var requests = Enumerable.Range(1, 5).Select(id => (WriteModel<BsonDocument>)new UpdateOneModel<BsonDocument>(
            Builders<BsonDocument>.Filter.Eq("_id", id), Builders<BsonDocument>.Update.Set("name", "written")) { IsUpsert = true }).ToList();
        await writes.BulkWrite(_models, requests, CancellationToken.None);
        _count = await Count();
        _receipts = await _database.GetCollection<BsonDocument>(FenceCollections.Receipts).CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty);
    }

    [Fact] void should_write_every_document() => _count.ShouldEqual(5L);
    [Fact] void should_commit_three_capped_transactions() => _receipts.ShouldEqual(3L);
}
