// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Occurrences.given;

namespace Cratis.Chronicle.Occurrences.for_OccurrenceAttempt.when_acknowledging_renewal;

public class and_the_reply_arrives_at_expiry : Specification
{
    readonly ManualOccurrenceClock _clock = new();
    bool _accepted;
    bool _cancelled;
    bool _admittedAfterwards;

    async Task Because()
    {
        await using var lifetime = new OccurrenceAttemptLifetime(_clock, _clock.GetTimestamp(), CancellationToken.None);
        _clock.Advance(TimeSpan.FromSeconds(20));
        var renewal = lifetime.Admit();
        _clock.Advance(TimeSpan.FromSeconds(40), deliverCallbacks: false);
        _accepted = lifetime.Accept(renewal);
        _cancelled = lifetime.Token.IsCancellationRequested;
        _admittedAfterwards = lifetime.Admit() is not null;
        foreach (var callback in _clock.CaptureDueCallbacks()) callback();
    }

    [Fact] void should_reject_the_reply_even_before_the_watchdog_callback_runs() => _accepted.ShouldBeFalse();
    [Fact] void should_cancel_the_lifetime() => _cancelled.ShouldBeTrue();
    [Fact] void should_not_revive_the_budget() => _admittedAfterwards.ShouldBeFalse();
}
