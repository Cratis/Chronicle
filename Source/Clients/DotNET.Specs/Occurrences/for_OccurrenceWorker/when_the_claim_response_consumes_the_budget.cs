// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Commands;
using Cratis.Chronicle.Contracts.Occurrences;
using Cratis.Chronicle.Occurrences.given;
using Microsoft.Extensions.Logging.Abstractions;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Occurrences.for_OccurrenceWorker;

public class when_the_claim_response_consumes_the_budget : an_occurrence_client
{
    readonly ManualOccurrenceClock _clock = new();
    int _handlerInvocations;

    void Establish()
    {
        _handler.OnHandle = (_, _, _) =>
        {
            _handlerInvocations++;
            return Task.CompletedTask;
        };
        _service.ClaimOccurrences(Arg.Any<ClaimOccurrencesRequest>(), Arg.Any<CallContext>()).Returns(_ =>
        {
            _clock.Advance(TimeSpan.FromSeconds(60), deliverCallbacks: false);
            _notifications.OnCompleted();
            return CommandResult<ClaimOccurrencesResponse>.Success(Guid.NewGuid(), new() { Claims = [_claim] });
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

    [Fact] void should_not_activate_a_handler() => _activator.DidNotReceive().Activate(Arg.Any<IServiceProvider>(), typeof(TestOccurrenceHandler));
    [Fact] void should_not_invoke_a_handler() => _handlerInvocations.ShouldEqual(0);
    [Fact] void should_not_complete() => _service.DidNotReceive().CompleteOccurrence(Arg.Any<CompleteOccurrenceRequest>(), Arg.Any<CallContext>());
    [Fact] void should_not_fail() => _service.DidNotReceive().FailOccurrence(Arg.Any<FailOccurrenceRequest>(), Arg.Any<CallContext>());
    [Fact] void should_not_renew() => _service.DidNotReceive().RenewOccurrenceLease(Arg.Any<RenewOccurrenceLeaseRequest>(), Arg.Any<CallContext>());
}
