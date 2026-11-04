// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Occurrences.given;

namespace Cratis.Chronicle.Occurrences.for_OccurrenceAttemptLifetime.when_tracking_expiry;

public class and_it_is_abandoned : Specification
{
    bool _cancelled;
    bool _expired;

    async Task Because()
    {
        var clock = new ManualOccurrenceClock();
        await using var lifetime = new OccurrenceAttemptLifetime(clock, clock.GetTimestamp(), CancellationToken.None);
        lifetime.Abandon();
        clock.Advance(TimeSpan.FromSeconds(60));
        _cancelled = lifetime.Token.IsCancellationRequested;
        _expired = lifetime.Expired;
    }

    [Fact] void should_cancel_the_attempt() => _cancelled.ShouldBeTrue();
    [Fact] void should_not_be_expired() => _expired.ShouldBeFalse();
}
