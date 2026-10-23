// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.Generations.Fencing.for_FencedBatchExecutor.when_executing;

public class and_the_epoch_has_moved_on : given.a_fenced_batch_executor
{
    Exception _error;

    protected override void ConfigureTransaction(IFenceTransaction transaction, int attempt) =>
        transaction.Admit(Arg.Any<FenceToken>(), Arg.Any<FenceBatchId>(), Arg.Any<CancellationToken>()).Returns(false);

    async Task Because() => _error = await Catch.Exception(Execute);

    [Fact] void should_refuse_the_batch() => _error.ShouldBeOfExactType<FenceRevoked>();
    [Fact] void should_not_run_the_writes() => _writesCalled.ShouldEqual(0);
    [Fact] void should_abort() => Transaction(0).Received(1).Abort();
    [Fact] void should_not_commit() => Transaction(0).DidNotReceive().Commit(Arg.Any<CancellationToken>());
    [Fact] void should_not_try_again() => _transactions.Count.ShouldEqual(1);
}
