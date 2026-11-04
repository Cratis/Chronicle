// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Occurrences;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Occurrences.for_OccurrenceAttempt.when_settling_after_abandonment;

public class and_the_handler_ignores_cancellation : given.a_running_attempt
{
    void Establish() => _handler.OnHandle = (_, _, token) =>
    {
        _handlerToken = token;
        _entered.TrySetResult();
        return _release.Task;
    };

    async Task Because()
    {
        await Start();
        _clock.Advance(TimeSpan.FromSeconds(60), deliverCallbacks: false);
        await Finish();
    }

    [Fact] void should_recheck_expiry_before_settlement_even_without_a_watchdog_callback() => _handlerToken.IsCancellationRequested.ShouldBeTrue();
    [Fact] void should_dispose_the_late_handler() => _handler.Disposed.ShouldBeTrue();
    [Fact] void should_not_complete() => _service.DidNotReceive().CompleteOccurrence(Arg.Any<CompleteOccurrenceRequest>(), Arg.Any<CallContext>());
    [Fact] void should_not_fail() => _service.DidNotReceive().FailOccurrence(Arg.Any<FailOccurrenceRequest>(), Arg.Any<CallContext>());
}
