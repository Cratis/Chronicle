// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Occurrences;
using Cratis.Chronicle.Occurrences.given;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Occurrences.for_OccurrenceAttempt.when_the_budget_expires;

public class and_the_claim_is_already_expired : an_occurrence_client
{
    readonly ManualOccurrenceClock _clock = new();
    int _handlerInvocations;

    void Establish() => _handler.OnHandle = (_, _, _) =>
    {
        _handlerInvocations++;
        return Task.CompletedTask;
    };

    async Task Because()
    {
        var claimedAt = _clock.GetTimestamp();
        _clock.Advance(TimeSpan.FromSeconds(60), deliverCallbacks: false);
        await CreateAttempt(_clock).Run(_claim, claimedAt, CancellationToken.None);
    }

    [Fact] void should_not_activate_a_handler() => _activator.DidNotReceive().Activate(Arg.Any<IServiceProvider>(), typeof(TestOccurrenceHandler));
    [Fact] void should_not_invoke_a_handler() => _handlerInvocations.ShouldEqual(0);
    [Fact] void should_not_complete() => _service.DidNotReceive().CompleteOccurrence(Arg.Any<CompleteOccurrenceRequest>(), Arg.Any<CallContext>());
    [Fact] void should_not_fail() => _service.DidNotReceive().FailOccurrence(Arg.Any<FailOccurrenceRequest>(), Arg.Any<CallContext>());
    [Fact] void should_not_renew() => _service.DidNotReceive().RenewOccurrenceLease(Arg.Any<RenewOccurrenceLeaseRequest>(), Arg.Any<CallContext>());
}
