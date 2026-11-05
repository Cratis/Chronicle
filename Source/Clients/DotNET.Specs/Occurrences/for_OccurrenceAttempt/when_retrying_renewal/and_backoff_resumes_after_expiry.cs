// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Occurrences;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Occurrences.for_OccurrenceAttempt.when_retrying_renewal;

public class and_backoff_resumes_after_expiry : given.a_running_attempt
{
    void Establish() => _renew = _ => Unavailable();

    async Task Because()
    {
        await RenewAtTwentySeconds();
        var backoff = await _clock.WaitForTimer(TimeSpan.FromSeconds(1));
        _clock.Advance(TimeSpan.FromSeconds(40), deliverCallbacks: false);
        backoff.CaptureCallback()();
        await _running.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
    }

    [Fact] void should_recheck_the_budget_before_retry_admission() => _renewalCount.ShouldEqual(1);
    [Fact] void should_cancel_the_handler_without_a_watchdog_callback() => _handlerToken.IsCancellationRequested.ShouldBeTrue();
    [Fact] void should_not_complete() => _service.DidNotReceive().CompleteOccurrence(Arg.Any<CompleteOccurrenceRequest>(), Arg.Any<CallContext>());
    [Fact] void should_not_fail() => _service.DidNotReceive().FailOccurrence(Arg.Any<FailOccurrenceRequest>(), Arg.Any<CallContext>());
}
