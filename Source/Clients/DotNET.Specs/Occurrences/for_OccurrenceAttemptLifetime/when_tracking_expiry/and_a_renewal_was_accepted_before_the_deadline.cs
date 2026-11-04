// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Occurrences.given;

namespace Cratis.Chronicle.Occurrences.for_OccurrenceAttemptLifetime.when_tracking_expiry;

public class and_a_renewal_was_accepted_before_the_deadline : Specification
{
    bool _accepted;
    bool _cancelled;
    bool _expired;

    async Task Because()
    {
        var clock = new ManualOccurrenceClock();
        await using var lifetime = new OccurrenceAttemptLifetime(clock, clock.GetTimestamp(), CancellationToken.None);
        clock.Advance(TimeSpan.FromSeconds(20));
        _accepted = lifetime.Accept(lifetime.Admit()!);
        clock.Advance(TimeSpan.FromSeconds(50));
        await clock.WaitForTimer(TimeSpan.FromSeconds(10));
        _cancelled = lifetime.Token.IsCancellationRequested;
        _expired = lifetime.Expired;
    }

    [Fact] void should_accept_the_renewal() => _accepted.ShouldBeTrue();
    [Fact] void should_keep_the_attempt_running() => _cancelled.ShouldBeFalse();
    [Fact] void should_not_be_expired() => _expired.ShouldBeFalse();
}
