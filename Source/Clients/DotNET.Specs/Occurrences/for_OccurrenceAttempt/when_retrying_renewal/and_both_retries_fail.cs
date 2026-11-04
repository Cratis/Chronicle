// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Occurrences;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Occurrences.for_OccurrenceAttempt.when_retrying_renewal;

public class and_both_retries_fail : given.a_running_attempt
{
    void Establish() => _renew = _ => Unavailable();

    async Task Because()
    {
        await RenewAtTwentySeconds();
        if (await AdvanceRetry()) await AdvanceRetry();
        await _running.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        _clock.Advance(TimeSpan.FromMinutes(2));
    }

    [Fact] void should_stop_after_three_requests() => _renewalCount.ShouldEqual(3);
    [Fact] void should_space_the_attempts_by_one_second() => _renewalTimes.ShouldEqual([TimeSpan.FromSeconds(20).Ticks, TimeSpan.FromSeconds(21).Ticks, TimeSpan.FromSeconds(22).Ticks]);
    [Fact] void should_cancel_the_handler() => _handlerToken.IsCancellationRequested.ShouldBeTrue();
    [Fact] void should_dispose_the_handler() => _handler.Disposed.ShouldBeTrue();
    [Fact] void should_not_complete() => _service.DidNotReceive().CompleteOccurrence(Arg.Any<CompleteOccurrenceRequest>(), Arg.Any<CallContext>());
    [Fact] void should_not_fail() => _service.DidNotReceive().FailOccurrence(Arg.Any<FailOccurrenceRequest>(), Arg.Any<CallContext>());
}
