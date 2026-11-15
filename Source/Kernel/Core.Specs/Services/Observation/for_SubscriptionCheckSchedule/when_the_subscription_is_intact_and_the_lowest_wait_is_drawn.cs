// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Services.Observation.for_SubscriptionCheckSchedule;

/// <summary>
/// Subscriptions registered together would all be checked in the same second of every interval; each wait is drawn
/// between three quarters of the safety-net interval and all of it.
/// </summary>
public class when_the_subscription_is_intact_and_the_lowest_wait_is_drawn : given.a_schedule
{
    protected override double Draw => 0;

    void Establish() => _schedule.Checked(true);

    void Because() => _next = _schedule.NextCheckAfter(Now);

    [Fact] void should_check_after_three_quarters_of_the_safety_net_interval() => _next.ShouldEqual(SafetyNet * 0.75);
}
