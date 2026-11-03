// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Queries;
using Cratis.Chronicle.Events;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_getting_tail_sequence_number_with_cancellation;

public class and_the_rpc_ignores_cancellation : given.an_asynchronous_event_sequence
{
    TaskCompletionSource _started;
    TaskCompletionSource<QueryResult<Contracts.Sequences.EventSequenceTailResponse>> _response;
    bool _completedAfterCancellation;
    EventSequenceNumber _result;

    void Establish()
    {
        _started = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _response = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _sequences.TailSequenceNumber(Arg.Any<Contracts.Sequences.TailSequenceNumberRequest>(), Arg.Any<CallContext>()).Returns(_ =>
        {
            _started.TrySetResult();
            return _response.Task;
        });
    }

    async Task Because()
    {
        var tail = _eventSequence.GetTailSequenceNumberWithCancellation(_cancellation.Token);
        try
        {
            await _started.Task.WaitAsync(TimeSpan.FromSeconds(10));
            await _cancellation.CancelAsync();
            _completedAfterCancellation = tail.IsCompleted;
        }
        finally
        {
            _response.TrySetResult(QueryResult<Contracts.Sequences.EventSequenceTailResponse>.Success(Guid.NewGuid(), new() { SequenceNumber = 75UL }));
            _result = await tail.WaitAsync(TimeSpan.FromSeconds(10));
        }
    }

    [Fact] void should_not_detach_the_rpc_on_cancellation() => _completedAfterCancellation.ShouldBeFalse();
    [Fact] void should_return_when_the_rpc_completes() => _result.ShouldEqual((EventSequenceNumber)75UL);
}
