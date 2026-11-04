// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Commands;
using Cratis.Chronicle.Contracts.Occurrences;
using Grpc.Core;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Occurrences.for_OccurrenceAttempt.when_acknowledging_renewal;

public class and_the_session_ends_before_the_reply : given.a_running_attempt
{
    readonly TaskCompletionSource<CommandResult<CompleteOccurrenceResponse>> _reply = new(TaskCreationOptions.RunContinuationsAsynchronously);
    bool _stoppedBeforeReply;

    void Establish() => _renew = _ => _reply.Task;

    async Task Because()
    {
        await RenewAtTwentySeconds();
        try
        {
            await _session.CancelAsync();
            await _running.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
            _stoppedBeforeReply = !_reply.Task.IsCompleted;
        }
        finally
        {
            _reply.TrySetException(new RpcException(new Status(StatusCode.Unavailable, "late transport failure")));
        }
        _clock.Advance(TimeSpan.FromMinutes(2));
    }

    [Fact] void should_stop_without_waiting_for_the_rpc() => _stoppedBeforeReply.ShouldBeTrue();
    [Fact] void should_cancel_the_rpc() => _renewalToken.IsCancellationRequested.ShouldBeTrue();
    [Fact] void should_not_retry_the_late_fault() => _renewalCount.ShouldEqual(1);
    [Fact] void should_not_complete() => _service.DidNotReceive().CompleteOccurrence(Arg.Any<CompleteOccurrenceRequest>(), Arg.Any<CallContext>());
    [Fact] void should_not_fail() => _service.DidNotReceive().FailOccurrence(Arg.Any<FailOccurrenceRequest>(), Arg.Any<CallContext>());
}
