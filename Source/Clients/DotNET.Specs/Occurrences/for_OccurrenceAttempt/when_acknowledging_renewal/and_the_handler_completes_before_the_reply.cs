// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Commands;
using Cratis.Chronicle.Contracts.Occurrences;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Occurrences.for_OccurrenceAttempt.when_acknowledging_renewal;

public class and_the_handler_completes_before_the_reply : given.a_running_attempt
{
    readonly TaskCompletionSource<CommandResult<CompleteOccurrenceResponse>> _reply = new(TaskCreationOptions.RunContinuationsAsynchronously);
    bool _stoppedBeforeReply;

    void Establish() => _renew = _ => _reply.Task;

    async Task Because()
    {
        await RenewAtTwentySeconds();
        try
        {
            await Finish();
            _stoppedBeforeReply = !_reply.Task.IsCompleted;
        }
        finally
        {
            _reply.TrySetResult(Renewed());
        }
    }

    [Fact] void should_stop_without_waiting_for_renewal_acknowledgement() => _stoppedBeforeReply.ShouldBeTrue();
    [Fact] void should_complete_once() => _service.Received(1).CompleteOccurrence(Arg.Any<CompleteOccurrenceRequest>(), Arg.Any<CallContext>());
    [Fact] void should_not_fail() => _service.DidNotReceive().FailOccurrence(Arg.Any<FailOccurrenceRequest>(), Arg.Any<CallContext>());
    [Fact] void should_cancel_the_pending_renewal() => _renewalToken.IsCancellationRequested.ShouldBeTrue();
}
