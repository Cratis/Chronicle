// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.MongoDB.EventSequences;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Generations.Fencing.for_UnfencedSinkWrites;

[Collection(ReplicaSetMongoDBCollection.Name)]
public class when_an_unguarded_upsert_collides(ReplicaSetMongoDBFixture fixture) : for_FencedSinkWrites.given(fixture)
{
    Exception _error;
    long _count;

    async Task Establish() => await _models.InsertOneAsync(new BsonDocument("_id", 2));

    async Task Because()
    {
        _error = await Catch.Exception(() => new UnfencedSinkWrites().BulkWrite(_models, [Insert(1), Insert(2), Insert(3)], CancellationToken.None));
        _count = await Count();
    }

    [Fact] void should_report_the_duplicate() => _error.ShouldBeOfExactType<MongoBulkWriteException<BsonDocument>>();
    [Fact] void should_stop_at_the_failed_request() => _count.ShouldEqual(2);
}
