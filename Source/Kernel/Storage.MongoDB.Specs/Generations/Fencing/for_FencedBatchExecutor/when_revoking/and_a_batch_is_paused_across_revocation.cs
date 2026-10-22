// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.MongoDB.EventSequences;

namespace Cratis.Chronicle.Storage.MongoDB.Generations.Fencing.for_FencedBatchExecutor.when_revoking;

/// <summary>
/// Pauses a batch at a point after it wrote the gate record, revokes while it is paused, then resumes it.
/// </summary>
/// <param name="fixture">The <see cref="ReplicaSetMongoDBFixture"/>.</param>
/// <param name="point">Where the batch pauses.</param>
public abstract class and_a_batch_is_paused_across_revocation(ReplicaSetMongoDBFixture fixture, FencePausePoint point) : Fencing.given.a_fenced_writer_against_a_replica_set(fixture)
{
    protected bool _revocationWaitedForTheBatch;
    protected Exception? _staleError;

    async Task Because()
    {
        await CommitBeforeRevocation("before");
        _probe.Arm(point);
        var paused = WriteBatch("paused");
        await _probe.Reached;
        var revocation = Revoke();
        await Task.Delay(500);
        _revocationWaitedForTheBatch = !revocation.IsCompleted;
        _probe.Release();
        await paused;
        _committedBeforeRevocation.Add("paused");
        await revocation;
        _staleError = await StaleBatch("after");
        await Judge();
    }

    [Fact] protected void should_make_revocation_wait_for_the_admitted_batch() => _revocationWaitedForTheBatch.ShouldBeTrue();
    [Fact] protected void should_refuse_a_batch_after_revocation() => _staleError.ShouldBeOfExactType<FenceRevoked>();
    [Fact] protected void should_leave_a_consistent_history() => _violations.ShouldBeEmpty();
}
