// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.MongoDB.EventSequences;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Generations.Fencing.for_FencedSinkWrites.when_bulk_writing;

[Collection(ReplicaSetMongoDBCollection.Name)]
public class and_it_fails_part_way(ReplicaSetMongoDBFixture fixture) : given(fixture)
{
    Exception? _error;
    long _count;

    async Task Because()
    {
        await _models.InsertOneAsync(new BsonDocument("_id", 3));
        _error = await Catch.Exception(() => _writes.BulkWrite(_models, [Insert(1), Insert(2), Insert(3), Insert(4)], CancellationToken.None));
        _count = await Count();
    }

    [Fact] void should_surface_the_write_error() => _error.ShouldBeOfExactType<MongoBulkWriteException<BsonDocument>>();
    [Fact] void should_keep_the_requests_before_the_failure_like_an_ordered_bulk_write() => _count.ShouldEqual(3L);
}
