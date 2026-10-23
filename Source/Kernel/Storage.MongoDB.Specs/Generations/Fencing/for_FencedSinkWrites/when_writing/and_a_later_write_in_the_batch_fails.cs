// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.MongoDB.EventSequences;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Generations.Fencing.for_FencedSinkWrites.when_writing;

[Collection(ReplicaSetMongoDBCollection.Name)]
public class and_a_later_write_in_the_batch_fails(ReplicaSetMongoDBFixture fixture) : given(fixture)
{
    Exception? _error;
    long _count;

    async Task Because()
    {
        _error = await Catch.Exception(() => _writes.InBatch(async () =>
        {
            await _writes.UpdateOne(_models, Builders<BsonDocument>.Filter.Eq("_id", 1), Builders<BsonDocument>.Update.Set("name", "first"), new UpdateOptions { IsUpsert = true });
            await _writes.UpdateOne(_models, Builders<BsonDocument>.Filter.Eq("_id", 2), Builders<BsonDocument>.Update.Set("_id", 3), new UpdateOptions { IsUpsert = true });
            return 0;
        }));
        _count = await Count();
    }

    [Fact] void should_fail() => _error.ShouldNotBeNull();
    [Fact] void should_roll_back_the_earlier_write_with_it() => _count.ShouldEqual(0L);
}
