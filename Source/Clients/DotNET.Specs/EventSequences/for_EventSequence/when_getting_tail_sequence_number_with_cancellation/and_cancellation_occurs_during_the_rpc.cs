// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Queries;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_getting_tail_sequence_number_with_cancellation;

public class and_cancellation_occurs_during_the_rpc : given.an_asynchronous_event_sequence
{
    TaskCompletionSource _started;
    TaskCompletionSource _observed;
    TaskCompletionSource _acknowledge;
    Exception _error;
    CancellationToken _observedToken;
    bool _completedBeforeAcknowledgement;

    void Establish()
    {
        _started = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _observed = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _acknowledge = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _sequences.TailSequenceNumber(Arg.Any<Contracts.Sequences.TailSequenceNumberRequest>(), Arg.Any<CallContext>()).Returns(call => Pending(call.Arg<CallContext>()));
    }

    async Task<QueryResult<Contracts.Sequences.EventSequenceTailResponse>> Pending(CallContext context)
    {
        _observedToken = context.CancellationToken;
        await using var registration = context.CancellationToken.Register(() => _observed.TrySetResult());
        _started.TrySetResult();
        await _acknowledge.Task.WaitAsync(TimeSpan.FromSeconds(10));
        context.CancellationToken.ThrowIfCancellationRequested();
        return QueryResult<Contracts.Sequences.EventSequenceTailResponse>.Success(Guid.NewGuid(), new() { SequenceNumber = 75UL });
    }

    async Task Because()
    {
        var tail = _eventSequence.GetTailSequenceNumberWithCancellation(_cancellation.Token);
        try
        {
            await _started.Task.WaitAsync(TimeSpan.FromSeconds(10));
            await _cancellation.CancelAsync();
            await _observed.Task.WaitAsync(TimeSpan.FromSeconds(10));
            _completedBeforeAcknowledgement = tail.IsCompleted;
            _acknowledge.TrySetResult();
            _error = await Catch.Exception(() => tail.WaitAsync(TimeSpan.FromSeconds(10)));
        }
        finally
        {
            _acknowledge.TrySetResult();
            await Catch.Exception(() => tail.WaitAsync(TimeSpan.FromSeconds(10)));
        }
    }

    [Fact] void should_forward_the_caller_token_to_the_in_flight_rpc() => _observedToken.ShouldEqual(_cancellation.Token);
    [Fact] void should_wait_for_rpc_acknowledgement() => _completedBeforeAcknowledgement.ShouldBeFalse();
    [Fact] void should_surface_rpc_cancellation() => _error.ShouldBeAssignableFrom<OperationCanceledException>();
    [Fact] void should_preserve_the_caller_token() => ((OperationCanceledException)_error).CancellationToken.ShouldEqual(_cancellation.Token);
}
