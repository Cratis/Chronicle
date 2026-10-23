// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.Generations.Fencing.for_FencedBatchExecutor.when_executing;

public class and_the_epoch_is_current : given.a_fenced_batch_executor
{
    string _result;

    async Task Because() => _result = await Execute();

    [Fact] void should_return_the_result_of_the_writes() => _result.ShouldEqual("written");
    [Fact] void should_admit_through_the_gate_with_the_token() => Transaction(0).Received(1).Admit(_token, Arg.Any<FenceBatchId>(), Arg.Any<CancellationToken>());
    [Fact] void should_write_the_receipt() => Transaction(0).Received(1).WriteReceipt(_token, Arg.Any<FenceBatchId>(), Arg.Any<CancellationToken>());
    [Fact] void should_commit() => Transaction(0).Received(1).Commit(Arg.Any<CancellationToken>());
    [Fact] void should_write_once() => _writesCalled.ShouldEqual(1);
    [Fact] void should_admit_before_writing_the_receipt() => Received.InOrder(() =>
    {
        Transaction(0).Admit(_token, Arg.Any<FenceBatchId>(), Arg.Any<CancellationToken>());
        Transaction(0).WriteReceipt(_token, Arg.Any<FenceBatchId>(), Arg.Any<CancellationToken>());
        Transaction(0).Commit(Arg.Any<CancellationToken>());
    });
}
