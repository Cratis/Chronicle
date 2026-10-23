// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.MongoDB.EventSequences;

namespace Cratis.Chronicle.Storage.MongoDB.Generations.Fencing.for_FencedBatchExecutor.when_the_commit_result_is_unknown;

[Collection(ReplicaSetMongoDBCollection.Name)]
public class and_the_owner_was_revoked_before_resolution(ReplicaSetMongoDBFixture fixture) : a_batch_with_an_unknown_commit(fixture, UnknownCommitMode.AbortedButReplyLost)
{
    protected override Task BetweenCommitAndReply() => Revoke();

    [Fact] void should_refuse_the_batch() => _error.ShouldBeOfExactType<FenceRevoked>();
    [Fact] void should_not_write_the_batch() => DocumentsWritten.ShouldEqual(0);
}
