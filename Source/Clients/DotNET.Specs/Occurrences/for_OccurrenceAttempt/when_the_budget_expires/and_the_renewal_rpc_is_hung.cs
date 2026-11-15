// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Commands;
using Cratis.Chronicle.Contracts.Occurrences;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Occurrences.for_OccurrenceAttempt.when_the_budget_expires;

public class and_the_renewal_rpc_is_hung : given.a_running_attempt
{
    readonly TaskCompletionSource<CommandResult<CompleteOccurrenceResponse>> _reply = new(TaskCreationOptions.RunContinuationsAsynchronously);
    bool _stoppedBeforeReply;
    bool _handlerCancelledBeforeReply;
    bool _rpcCancelledBeforeReply;

    void Establish() => _renew = _ => _reply.Task;

    async Task Because()
    {
        await RenewAtTwentySeconds();
        _clock.Advance(TimeSpan.FromSeconds(40));
        try
        {
            await _running.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
            _stoppedBeforeReply = true;
        }
        catch (TimeoutException)
        {
            // Record the missing independent expiry, then release the ignored transport for cleanup.
            _stoppedBeforeReply = false;
        }
        finally
        {
            _handlerCancelledBeforeReply = _handlerToken.IsCancellationRequested;
            _rpcCancelledBeforeReply = _renewalToken.IsCancellationRequested;
            _reply.TrySetResult(Renewed());
            await Finish();
        }
        _clock.Advance(TimeSpan.FromMinutes(2));
    }

    [Fact] void should_stop_without_waiting_for_the_transport() => _stoppedBeforeReply.ShouldBeTrue();
    [Fact] void should_cancel_the_handler_at_the_deadline() => _handlerCancelledBeforeReply.ShouldBeTrue();
    [Fact] void should_cancel_the_rpc_at_the_deadline() => _rpcCancelledBeforeReply.ShouldBeTrue();
    [Fact] void should_dispose_the_cancelled_handler() => _handler.Disposed.ShouldBeTrue();
    [Fact] void should_not_start_another_renewal() => _renewalCount.ShouldEqual(1);
    [Fact] void should_not_complete_after_the_late_reply() => _service.DidNotReceive().CompleteOccurrence(Arg.Any<CompleteOccurrenceRequest>(), Arg.Any<CallContext>());
    [Fact] void should_not_fail_after_the_late_reply() => _service.DidNotReceive().FailOccurrence(Arg.Any<FailOccurrenceRequest>(), Arg.Any<CallContext>());
}
