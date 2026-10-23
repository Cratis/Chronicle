// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.MongoDB.EventSequences;

namespace Cratis.Chronicle.Storage.MongoDB.Generations.Fencing.for_FencedBatchExecutor.when_revoking;

[Collection(ReplicaSetMongoDBCollection.Name)]
public class and_the_batch_is_paused_before_admission(ReplicaSetMongoDBFixture fixture) : Fencing.given.a_fenced_writer_against_a_replica_set(fixture)
{
    Exception? _error;

    async Task Because()
    {
        await CommitBeforeRevocation("before");
        if (FenceDisabled)
        {
            await Revoke();
            _error = await StaleBatch("paused");
        }
        else
        {
            _probe.Arm(FencePausePoint.BeforeAdmission);
            _revoked.Add("paused");
            var paused = WriteBatch("paused");
            await _probe.Reached;
            await Revoke();
            _probe.Release();
            _error = await Catch.Exception(() => paused);
        }

        await Judge();
    }

    [Fact] void should_refuse_the_batch() => _error.ShouldBeOfExactType<FenceRevoked>();
    [Fact] void should_leave_a_consistent_history() => _violations.ShouldBeEmpty();
}
