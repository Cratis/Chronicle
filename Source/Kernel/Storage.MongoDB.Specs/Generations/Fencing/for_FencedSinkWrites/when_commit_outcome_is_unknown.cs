// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.MongoDB.EventSequences;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Generations.Fencing.for_FencedSinkWrites;

[Collection(ReplicaSetMongoDBCollection.Name)]
public class when_commit_outcome_is_unknown(ReplicaSetMongoDBFixture fixture) : given(fixture)
{
    Exception? _error;
    long _count;
    UnknownCommitStore _unknown;

    async Task Because()
    {
        _unknown = new(_store, UnknownCommitMode.CommittedButReplyLost, () => Task.CompletedTask);
        using var writes = new FencedSinkWrites(new FixedFenceTokenSource(_token), new FencedBatchExecutor(_unknown, _policy, _probe));
        _error = await Catch.Exception(() => writes.UpdateOne(_models, Builders<BsonDocument>.Filter.Eq("_id", 1), Builders<BsonDocument>.Update.Set("name", "written"), new UpdateOptions { IsUpsert = true }));
        _count = await Count();
    }

    [Fact] void should_resolve_the_outcome() => _unknown.Resolutions.ShouldEqual(1);
    [Fact] void should_succeed_only_after_resolution() => _error.ShouldBeNull();
    [Fact] void should_write_once() => _count.ShouldEqual(1L);
}
