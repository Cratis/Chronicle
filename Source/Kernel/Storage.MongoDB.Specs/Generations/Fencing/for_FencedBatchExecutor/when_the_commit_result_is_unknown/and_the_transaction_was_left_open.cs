// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.MongoDB.EventSequences;

namespace Cratis.Chronicle.Storage.MongoDB.Generations.Fencing.for_FencedBatchExecutor.when_the_commit_result_is_unknown;

/// <summary>
/// The server still holds the batch open; resolution must wait for it to end rather than guess.
/// </summary>
/// <param name="fixture">The <see cref="ReplicaSetMongoDBFixture"/>.</param>
[Collection(ReplicaSetMongoDBCollection.Name)]
public class and_the_transaction_was_left_open(ReplicaSetMongoDBFixture fixture) : a_batch_with_an_unknown_commit(fixture, UnknownCommitMode.LeftOpenAndReplyLost)
{
    async Task Establish() => await SetTransactionLifetime(2);

    async Task Destroy() => await SetTransactionLifetime(60);

    [Fact] void should_succeed() => _error.ShouldBeNull();
    [Fact] void should_write_the_batch_exactly_once() => DocumentsWritten.ShouldEqual(DocumentsPerBatch);
}
