// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.MongoDB.EventSequences;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Generations.Fencing.for_FencedSinkWrites;

[Collection(ReplicaSetMongoDBCollection.Name)]
public class when_the_gate_was_revoked_before_commit(ReplicaSetMongoDBFixture fixture) : given(fixture)
{
    Exception? _error;
    long _count;

    async Task Because()
    {
        _probe.Arm(FencePausePoint.BeforeAdmission);
        var pending = Catch.Exception(() => _writes.UpdateOne(_models, Builders<BsonDocument>.Filter.Eq("_id", 1), Builders<BsonDocument>.Update.Set("name", "stale"), new UpdateOptions { IsUpsert = true }));
        await _probe.Reached.WaitAsync(TimeSpan.FromSeconds(30));
        await Revoke();
        _probe.Release();
        _error = await pending;
        _count = await Count();
    }

    [Fact] void should_surface_revocation() => _error.ShouldBeOfExactType<FenceRevoked>();
    [Fact] void should_not_write() => _count.ShouldEqual(0L);
}
