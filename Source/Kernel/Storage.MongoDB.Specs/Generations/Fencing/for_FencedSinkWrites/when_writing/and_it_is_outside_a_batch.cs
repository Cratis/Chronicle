// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.MongoDB.EventSequences;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Generations.Fencing.for_FencedSinkWrites.when_writing;

[Collection(ReplicaSetMongoDBCollection.Name)]
public class and_it_is_outside_a_batch(ReplicaSetMongoDBFixture fixture) : given(fixture)
{
    Exception? _error;

    async Task Because()
    {
        await Revoke();
        _error = await Catch.Exception(() => _writes.DeleteOne(_models, Builders<BsonDocument>.Filter.Eq("_id", 1)));
    }

    [Fact] void should_still_be_fenced() => _error.ShouldBeOfExactType<FenceRevoked>();
}
