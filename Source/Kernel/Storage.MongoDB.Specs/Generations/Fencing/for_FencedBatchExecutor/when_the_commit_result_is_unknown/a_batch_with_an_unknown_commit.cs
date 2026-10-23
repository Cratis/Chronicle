// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.MongoDB.EventSequences;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Generations.Fencing.for_FencedBatchExecutor.when_the_commit_result_is_unknown;

public abstract class a_batch_with_an_unknown_commit(ReplicaSetMongoDBFixture fixture, UnknownCommitMode mode) : Fencing.given.a_fenced_writer_against_a_replica_set(fixture)
{
    protected UnknownCommitStore _unknownStore;
    protected Exception? _error;

    protected virtual Task BetweenCommitAndReply() => Task.CompletedTask;

    async Task Because()
    {
        _unknownStore = new(_store, mode, BetweenCommitAndReply);
        _error = await Catch.Exception(() => WriteBatch("unknown", executor: new FencedBatchExecutor(_unknownStore, _policy, _probe)));
    }

    protected long DocumentsWritten => History.CountDocuments(FilterDefinition<BsonDocument>.Empty);
}
