// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.Generations.Fencing.for_FencedBatchExecutor.when_executing;

public class and_the_commit_result_is_unknown_and_the_epoch_moved_on : given.a_fenced_batch_executor
{
    Exception _error;

    protected override void ConfigureTransaction(IFenceTransaction transaction, int attempt) =>
        transaction.Commit(Arg.Any<CancellationToken>()).Returns(Task.FromException(Labeled(FenceRetryPolicy.UnknownTransactionCommitResult)));

    void Establish() => _store.Resolve(_token, Arg.Any<FenceBatchId>(), Arg.Any<CancellationToken>()).Returns(UnknownCommitResolution.Revoked);

    async Task Because() => _error = await Catch.Exception(Execute);

    [Fact] void should_refuse_the_batch() => _error.ShouldBeOfExactType<FenceRevoked>();
    [Fact] void should_not_run_the_batch_again() => _transactions.Count.ShouldEqual(1);
}
