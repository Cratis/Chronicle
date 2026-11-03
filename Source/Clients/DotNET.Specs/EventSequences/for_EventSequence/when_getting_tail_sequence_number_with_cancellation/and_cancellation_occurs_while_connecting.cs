// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_getting_tail_sequence_number_with_cancellation;

public class and_cancellation_occurs_while_connecting : given.an_asynchronous_event_sequence
{
    TaskCompletionSource<IServices> _pending;
    Exception _error;
    bool _completedBeforeCancellation;
    bool _connectionCompleted;

    void Establish()
    {
        _pending = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _asynchronous.Acquire = token => new(_pending.Task.WaitAsync(token));
    }

    async Task Because()
    {
        var tail = _eventSequence.GetTailSequenceNumberWithCancellation(_cancellation.Token);
        try
        {
            _completedBeforeCancellation = tail.IsCompleted;
            await _cancellation.CancelAsync();
            _error = await Catch.Exception(() => tail.WaitAsync(TimeSpan.FromSeconds(10)));
            _connectionCompleted = _pending.Task.IsCompleted;
        }
        finally
        {
            _pending.TrySetResult(services);
            await Catch.Exception(() => tail.WaitAsync(TimeSpan.FromSeconds(10)));
        }
    }

    [Fact] void should_return_control_while_connecting() => _completedBeforeCancellation.ShouldBeFalse();
    [Fact] void should_cancel_the_wait() => _error.ShouldBeAssignableFrom<OperationCanceledException>();
    [Fact] void should_preserve_the_token() => ((OperationCanceledException)_error).CancellationToken.ShouldEqual(_cancellation.Token);
    [Fact] void should_not_cancel_the_shared_connection() => _connectionCompleted.ShouldBeFalse();
    [Fact] void should_not_acquire_synchronously() => _ = _accessor.DidNotReceive().Services;
    [Fact] void should_not_call_the_rpc() => _rpcCalls.ShouldEqual(0);
}
