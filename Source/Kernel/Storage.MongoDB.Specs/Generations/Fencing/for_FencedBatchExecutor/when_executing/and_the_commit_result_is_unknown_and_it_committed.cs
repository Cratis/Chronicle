// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.Generations.Fencing.for_FencedBatchExecutor.when_executing;

public class and_the_commit_result_is_unknown_and_it_committed : given.a_fenced_batch_executor
{
    string _result;

    protected override void ConfigureTransaction(IFenceTransaction transaction, int attempt) =>
        transaction.Commit(Arg.Any<CancellationToken>()).Returns(Task.FromException(Labeled(FenceRetryPolicy.UnknownTransactionCommitResult)));

    void Establish() => _store.Resolve(_token, Arg.Any<FenceBatchId>(), Arg.Any<CancellationToken>()).Returns(UnknownCommitResolution.Committed);

    async Task Because() => _result = await Execute();

    [Fact] void should_resolve_the_batch() => _store.Received(1).Resolve(_token, Arg.Any<FenceBatchId>(), Arg.Any<CancellationToken>());
    [Fact] void should_not_run_the_batch_again() => _transactions.Count.ShouldEqual(1);
    [Fact] void should_return_the_result() => _result.ShouldEqual("written");
}
