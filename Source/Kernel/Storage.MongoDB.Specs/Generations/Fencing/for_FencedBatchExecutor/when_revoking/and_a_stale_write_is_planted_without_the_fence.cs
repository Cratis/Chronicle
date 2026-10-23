// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.MongoDB.EventSequences;

namespace Cratis.Chronicle.Storage.MongoDB.Generations.Fencing.for_FencedBatchExecutor.when_revoking;

/// <summary>
/// Non-vacuity: a stale write that bypasses the fence must be caught by the oracle, or every other case could pass for nothing.
/// </summary>
/// <param name="fixture">The <see cref="ReplicaSetMongoDBFixture"/>.</param>
[Collection(ReplicaSetMongoDBCollection.Name)]
public class and_a_stale_write_is_planted_without_the_fence(ReplicaSetMongoDBFixture fixture) : Fencing.given.a_fenced_writer_against_a_replica_set(fixture)
{
    async Task Because()
    {
        await CommitBeforeRevocation("before");
        await Revoke();
        await WriteUnfenced("planted", _token.Epoch);
        await Judge();
    }

    [Fact] void should_report_the_planted_write() => _violations.ShouldNotBeEmpty();
    [Fact] void should_report_every_planted_document() => _violations.Count.ShouldEqual(DocumentsPerBatch);
}
