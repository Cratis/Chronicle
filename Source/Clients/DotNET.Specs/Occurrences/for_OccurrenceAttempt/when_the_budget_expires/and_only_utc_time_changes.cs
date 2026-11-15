// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Occurrences.given;

namespace Cratis.Chronicle.Occurrences.for_OccurrenceAttempt.when_the_budget_expires;

public class and_only_utc_time_changes : Specification
{
    bool _activeAfterForwardJump;
    bool _activeAfterBackwardJump;

    async Task Because()
    {
        var clock = new ManualOccurrenceClock();
        await using var lifetime = new OccurrenceAttemptLifetime(clock, clock.GetTimestamp(), CancellationToken.None);
        clock.AdvanceUtc(TimeSpan.FromDays(365));
        _activeAfterForwardJump = lifetime.Admit() is not null;
        clock.AdvanceUtc(TimeSpan.FromDays(-730));
        _activeAfterBackwardJump = lifetime.Admit() is not null;
    }

    [Fact] void should_not_expire_after_a_forward_wall_clock_jump() => _activeAfterForwardJump.ShouldBeTrue();
    [Fact] void should_not_expire_after_a_backward_wall_clock_jump() => _activeAfterBackwardJump.ShouldBeTrue();
}
