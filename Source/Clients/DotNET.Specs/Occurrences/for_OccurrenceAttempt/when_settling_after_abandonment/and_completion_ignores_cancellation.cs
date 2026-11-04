// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Commands;
using Cratis.Chronicle.Contracts.Occurrences;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Occurrences.for_OccurrenceAttempt.when_settling_after_abandonment;

public class and_completion_ignores_cancellation : given.a_running_attempt
{
    readonly TaskCompletionSource _settling = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource<CommandResult<CompleteOccurrenceResponse>> _reply = new(TaskCreationOptions.RunContinuationsAsynchronously);
    bool _stoppedBeforeReply;

    void Establish() => _service.CompleteOccurrence(Arg.Any<CompleteOccurrenceRequest>(), Arg.Any<CallContext>()).Returns(_ =>
    {
        _settling.TrySetResult();
        return _reply.Task;
    });

    async Task Because()
    {
        await Start();
        _release.TrySetResult();
        try
        {
            await _settling.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
            await _session.CancelAsync();
            await _running.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
            _stoppedBeforeReply = !_reply.Task.IsCompleted;
        }
        finally
        {
            _reply.TrySetResult(Renewed());
        }
    }

    [Fact] void should_stop_without_waiting_for_settlement_transport() => _stoppedBeforeReply.ShouldBeTrue();
    [Fact] void should_not_retry_completion() => _service.Received(1).CompleteOccurrence(Arg.Any<CompleteOccurrenceRequest>(), Arg.Any<CallContext>());
    [Fact] void should_not_translate_unknown_completion_into_failure() => _service.DidNotReceive().FailOccurrence(Arg.Any<FailOccurrenceRequest>(), Arg.Any<CallContext>());
}
