// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Commands;
using Cratis.Chronicle.Contracts.Occurrences;
using Cratis.Chronicle.Occurrences.given;
using Microsoft.Extensions.Logging.Abstractions;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Occurrences.for_OccurrenceWorker;

public class when_the_session_ends_during_claim : an_occurrence_client
{
    readonly ManualOccurrenceClock _clock = new();
    readonly TaskCompletionSource _claimed = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource<CommandResult<ClaimOccurrencesResponse>> _reply = new(TaskCreationOptions.RunContinuationsAsynchronously);
    bool _stoppedBeforeReply;
    CancellationToken _claimToken;
    int _handlerInvocations;

    void Establish()
    {
        _handler.OnHandle = (_, _, _) =>
        {
            _handlerInvocations++;
            return Task.CompletedTask;
        };
        _service.ClaimOccurrences(Arg.Any<ClaimOccurrencesRequest>(), Arg.Any<CallContext>()).Returns(call =>
        {
            _claimToken = call.Arg<CallContext>().CancellationToken;
            _claimed.TrySetResult();
            return _reply.Task;
        });
    }

    async Task Because()
    {
        using var session = new CancellationTokenSource();
        var worker = new OccurrenceWorker(_store, _claim.Trigger, CreateAttempt(_clock), _clock, NullLogger.Instance);
        var running = worker.Run(session.Token);
        try
        {
            await _claimed.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
            await session.CancelAsync();
            await running.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
            _stoppedBeforeReply = !_reply.Task.IsCompleted;
        }
        finally
        {
            _reply.TrySetResult(CommandResult<ClaimOccurrencesResponse>.Success(Guid.NewGuid(), new() { Claims = [_claim] }));
        }
    }

    [Fact] void should_stop_without_waiting_for_claim_transport() => _stoppedBeforeReply.ShouldBeTrue();
    [Fact] void should_cancel_the_claim_request() => _claimToken.IsCancellationRequested.ShouldBeTrue();
    [Fact] void should_remove_the_observation_subscription() => _notifications.HasObservers.ShouldBeFalse();
    [Fact] void should_not_activate_a_handler_for_the_late_response() => _activator.DidNotReceive().Activate(Arg.Any<IServiceProvider>(), typeof(TestOccurrenceHandler));
    [Fact] void should_not_invoke_a_handler_for_the_late_response() => _handlerInvocations.ShouldEqual(0);
    [Fact] void should_not_complete() => _service.DidNotReceive().CompleteOccurrence(Arg.Any<CompleteOccurrenceRequest>(), Arg.Any<CallContext>());
    [Fact] void should_not_fail() => _service.DidNotReceive().FailOccurrence(Arg.Any<FailOccurrenceRequest>(), Arg.Any<CallContext>());
}
