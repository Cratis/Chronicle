// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.MongoDB.EventSequences;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Generations.Fencing.for_FencedSinkWrites;

[Collection(ReplicaSetMongoDBCollection.Name)]
public class when_a_split_update_is_not_idempotent(ReplicaSetMongoDBFixture fixture) : given(fixture)
{
    Exception? _error;
    long _changed;

    async Task Because()
    {
        await _models.InsertManyAsync(Enumerable.Range(1, 3).Select(id => new BsonDocument { { "_id", id }, { "children", new BsonArray() } }));
        using var writes = new FencedSinkWrites(new FixedFenceTokenSource(_token), _executor, 2);
        _error = await Catch.Exception(() => writes.UpdateMany(_models, FilterDefinition<BsonDocument>.Empty, Builders<BsonDocument>.Update.Push("children", "child"), new UpdateOptions()));
        _changed = await _models.CountDocumentsAsync(Builders<BsonDocument>.Filter.AnyEq("children", "child"));
    }

    [Fact] void should_refuse_an_unsafe_retry() => _error.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_not_push_children() => _changed.ShouldEqual(0L);
}
