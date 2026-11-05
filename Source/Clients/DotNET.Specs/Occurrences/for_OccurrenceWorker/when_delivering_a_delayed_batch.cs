// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Commands;
using Cratis.Chronicle.Contracts.Occurrences;
using Cratis.Chronicle.Occurrences.given;
using Microsoft.Extensions.Logging.Abstractions;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Occurrences.for_OccurrenceWorker;

public class when_delivering_a_delayed_batch : an_occurrence_client
{
    readonly ManualOccurrenceClock _clock = new();
    int _claims;
    int _deliveries;

    void Establish()
    {
        _handler.OnHandle = (_, _, _) =>
        {
            _clock.Advance(TimeSpan.FromSeconds(++_deliveries == 1 ? 40 : 10), deliverCallbacks: false);
            return Task.CompletedTask;
        };
        _service.ClaimOccurrences(Arg.Any<ClaimOccurrencesRequest>(), Arg.Any<CallContext>()).Returns(_ =>
        {
            if (_claims++ > 0) return CommandResult<ClaimOccurrencesResponse>.Success(Guid.NewGuid(), new());

            _clock.Advance(TimeSpan.FromSeconds(10), deliverCallbacks: false);
            _notifications.OnCompleted();
            var second = new OccurrenceClaimDetails
            {
                Id = "second",
                Trigger = _claim.Trigger,
                Kind = _claim.Kind,
                Payload = _claim.Payload,
                SchemaRevision = _claim.SchemaRevision,
                RecordedAt = _claim.RecordedAt,
                Due = _claim.Due,
                Attempts = _claim.Attempts,
                Epoch = 4,
                LeaseExpiry = _claim.LeaseExpiry
            };
            return CommandResult<ClaimOccurrencesResponse>.Success(Guid.NewGuid(), new() { Claims = [_claim, second] });
        });
    }

    async Task Because()
    {
        using var session = new CancellationTokenSource();
        var worker = new OccurrenceWorker(_store, _claim.Trigger, CreateAttempt(_clock), _clock, NullLogger.Instance);
        var running = worker.Run(session.Token);
        await _clock.WaitForTimer(TimeSpan.FromSeconds(1));
        await session.CancelAsync();
        await running.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
    }

    [Fact] void should_deliver_both_claims() => _deliveries.ShouldEqual(2);
    [Fact] void should_complete_the_first_claim_before_the_original_deadline() => _service.Received(1).CompleteOccurrence(Arg.Is<CompleteOccurrenceRequest>(request => request.OccurrenceId == "occurrence"), Arg.Any<CallContext>());
    [Fact] void should_not_reset_the_budget_for_the_second_claim() => _service.DidNotReceive().CompleteOccurrence(Arg.Is<CompleteOccurrenceRequest>(request => request.OccurrenceId == "second"), Arg.Any<CallContext>());
    [Fact] void should_not_fail_the_expired_claim() => _service.DidNotReceive().FailOccurrence(Arg.Any<FailOccurrenceRequest>(), Arg.Any<CallContext>());
}
