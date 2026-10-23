// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.Generations.Fencing.for_FencedBatchExecutor.when_executing;

public class and_the_commit_result_is_unknown_and_it_did_not_commit : given.a_fenced_batch_executor
{
    List<FenceBatchId> _admitted;
    FenceBatchId _resolved;

    protected override void ConfigureTransaction(IFenceTransaction transaction, int attempt)
    {
        transaction.When(_ => _.Admit(Arg.Any<FenceToken>(), Arg.Any<FenceBatchId>(), Arg.Any<CancellationToken>())).Do(call => _admitted.Add(call.Arg<FenceBatchId>()));
        if (attempt == 0)
        {
            transaction.Commit(Arg.Any<CancellationToken>()).Returns(Task.FromException(Labeled(FenceRetryPolicy.UnknownTransactionCommitResult)));
        }
    }

    void Establish()
    {
        _admitted = [];
        _store.Resolve(_token, Arg.Do<FenceBatchId>(id => _resolved = id), Arg.Any<CancellationToken>()).Returns(UnknownCommitResolution.NotCommitted);
    }

    async Task Because() => await Execute();

    [Fact] void should_resolve_the_unknown_batch_before_trying_again() => _resolved.ShouldEqual(_admitted[0]);
    [Fact] void should_run_the_batch_again() => _writesCalled.ShouldEqual(2);
    [Fact] void should_try_again_as_a_new_batch() => _admitted[1].ShouldNotEqual(_admitted[0]);
    [Fact] void should_commit_the_second_attempt() => Transaction(1).Received(1).Commit(Arg.Any<CancellationToken>());
}
