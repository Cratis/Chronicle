// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Services.Observation.for_SubscriptionCheckSchedule;

public class when_the_safety_net_is_shorter_than_the_revise_interval : given.a_schedule
{
    void Establish()
    {
        _schedule = new(Revise, TimeSpan.FromSeconds(1), Settle);
        _schedule.Checked(true);
    }

    void Because() => _next = _schedule.NextCheckAfter(Now);

    [Fact] void should_check_after_the_revise_interval() => _next.ShouldEqual(Revise);
}
