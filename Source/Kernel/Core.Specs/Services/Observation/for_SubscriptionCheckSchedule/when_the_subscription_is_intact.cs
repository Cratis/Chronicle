// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Services.Observation.for_SubscriptionCheckSchedule;

public class when_the_subscription_is_intact : given.a_schedule
{
    void Establish() => _schedule.Checked(true);

    void Because() => _next = _schedule.NextCheckAfter(Now);

    [Fact] void should_check_after_the_safety_net_interval() => _next.ShouldEqual(SafetyNet);
}
