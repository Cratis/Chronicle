// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Commands;
using Cratis.Chronicle.Contracts.Occurrences;
using Cratis.Chronicle.Occurrences.given;
using Microsoft.Extensions.Logging.Abstractions;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Occurrences.for_OccurrenceWorker;

public class when_the_claim_response_leaves_one_second : an_occurrence_client
{
    readonly ManualOccurrenceClock _clock = new();
    readonly TaskCompletionSource _entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);
    CancellationToken _handlerToken;
    int _claims;
    int _deliveries;

    void Establish()
    {
        _handler.OnHandle = (_, _, token) =>
        {
            _handlerToken = token;
            _deliveries++;
            _entered.TrySetResult();
            return _release.Task;
        };
        _service.ClaimOccurrences(Arg.Any<ClaimOccurrencesRequest>(), Arg.Any<CallContext>()).Returns(_ =>
        {
            if (_claims++ > 0) return CommandResult<ClaimOccurrencesResponse>.Success(Guid.NewGuid(), new());

            _clock.Advance(TimeSpan.FromSeconds(59), deliverCallbacks: false);
            return CommandResult<ClaimOccurrencesResponse>.Success(Guid.NewGuid(), new() { Claims = [_claim] });
        });
    }

    async Task Because()
    {
        using var session = new CancellationTokenSource();
        var worker = new OccurrenceWorker(_store, _claim.Trigger, CreateAttempt(_clock), _clock, NullLogger.Instance);
        var running = worker.Run(session.Token);
        await _entered.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        var cancelled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var registration = _handlerToken.Register(() => cancelled.TrySetResult());
        _clock.Advance(TimeSpan.FromSeconds(1), deliverCallbacks: false);
        _release.TrySetResult();
        await cancelled.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        await session.CancelAsync();
        await running.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
    }

    [Fact] void should_admit_the_handler_with_the_remaining_budget() => _deliveries.ShouldEqual(1);
    [Fact] void should_not_grant_a_fresh_budget_at_receipt() => _service.DidNotReceive().CompleteOccurrence(Arg.Any<CompleteOccurrenceRequest>(), Arg.Any<CallContext>());
    [Fact] void should_not_fail_the_expired_claim() => _service.DidNotReceive().FailOccurrence(Arg.Any<FailOccurrenceRequest>(), Arg.Any<CallContext>());
    [Fact] void should_not_renew_after_the_one_second_budget() => _service.DidNotReceive().RenewOccurrenceLease(Arg.Any<RenewOccurrenceLeaseRequest>(), Arg.Any<CallContext>());
}
