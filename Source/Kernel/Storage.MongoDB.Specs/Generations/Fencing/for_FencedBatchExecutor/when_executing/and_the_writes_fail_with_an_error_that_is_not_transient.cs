// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.Generations.Fencing.for_FencedBatchExecutor.when_executing;

public class and_the_writes_fail_with_an_error_that_is_not_transient : given.a_fenced_batch_executor
{
    InvalidOperationException _failure;
    Exception _error;

    void Establish()
    {
        _failure = new InvalidOperationException("write failed");
        _writes = () => Task.FromException<string>(_failure);
    }

    async Task Because() => _error = await Catch.Exception(Execute);

    [Fact] void should_surface_the_failure() => _error.ShouldEqual(_failure);
    [Fact] void should_abort() => Transaction(0).Received(1).Abort();
    [Fact] void should_not_try_again() => _transactions.Count.ShouldEqual(1);
}
