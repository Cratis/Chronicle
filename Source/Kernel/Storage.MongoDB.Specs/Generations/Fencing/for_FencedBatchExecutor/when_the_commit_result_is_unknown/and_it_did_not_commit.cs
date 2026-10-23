// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.MongoDB.EventSequences;

namespace Cratis.Chronicle.Storage.MongoDB.Generations.Fencing.for_FencedBatchExecutor.when_the_commit_result_is_unknown;

[Collection(ReplicaSetMongoDBCollection.Name)]
public class and_it_did_not_commit(ReplicaSetMongoDBFixture fixture) : a_batch_with_an_unknown_commit(fixture, UnknownCommitMode.AbortedButReplyLost)
{
    [Fact] void should_succeed() => _error.ShouldBeNull();
    [Fact] void should_resolve_once() => _unknownStore.Resolutions.ShouldEqual(1);
    [Fact] void should_write_the_batch_exactly_once() => DocumentsWritten.ShouldEqual(DocumentsPerBatch);
}
