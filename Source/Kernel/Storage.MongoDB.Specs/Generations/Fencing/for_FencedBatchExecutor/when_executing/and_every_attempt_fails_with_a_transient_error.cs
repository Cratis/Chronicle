// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.Generations.Fencing.for_FencedBatchExecutor.when_executing;

public class and_every_attempt_fails_with_a_transient_error : given.a_fenced_batch_executor
{
    Exception _error;

    void Establish() => _writes = () => Task.FromException<string>(Labeled(FenceRetryPolicy.TransientTransactionError));

    async Task Because() => _error = await Catch.Exception(Execute);

    [Fact] void should_give_up() => _error.ShouldBeOfExactType<FencedBatchRetriesExhausted>();
    [Fact] void should_stop_at_the_policy_limit() => _transactions.Count.ShouldEqual(5);
    [Fact] void should_never_commit() => _transactions.ForEach(_ => _.DidNotReceive().Commit(Arg.Any<CancellationToken>()));
}
