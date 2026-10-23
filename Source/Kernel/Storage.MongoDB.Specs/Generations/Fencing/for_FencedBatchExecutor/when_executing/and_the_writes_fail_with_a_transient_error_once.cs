// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.Generations.Fencing.for_FencedBatchExecutor.when_executing;

public class and_the_writes_fail_with_a_transient_error_once : given.a_fenced_batch_executor
{
    string _result;

    void Establish() => _writes = () => _writesCalled == 1
        ? Task.FromException<string>(Labeled(FenceRetryPolicy.TransientTransactionError))
        : Task.FromResult("written");

    async Task Because() => _result = await Execute();

    [Fact] void should_start_the_batch_again() => _transactions.Count.ShouldEqual(2);
    [Fact] void should_abort_the_failed_attempt() => Transaction(0).Received(1).Abort();
    [Fact] void should_not_commit_the_failed_attempt() => Transaction(0).DidNotReceive().Commit(Arg.Any<CancellationToken>());
    [Fact] void should_commit_the_second_attempt() => Transaction(1).Received(1).Commit(Arg.Any<CancellationToken>());
    [Fact] void should_return_the_result_of_the_second_attempt() => _result.ShouldEqual("written");
}
