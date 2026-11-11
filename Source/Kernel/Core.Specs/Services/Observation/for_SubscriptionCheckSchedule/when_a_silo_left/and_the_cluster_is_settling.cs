// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Services.Observation.for_SubscriptionCheckSchedule.when_a_silo_left;

public class and_the_cluster_is_settling : given.a_schedule
{
    void Establish()
    {
        _schedule.Checked(true);
        _schedule.SiloLeft(Now);
    }

    void Because() => _next = _schedule.NextCheckAfter(Now + Settle - TimeSpan.FromSeconds(1));

    [Fact] void should_check_an_intact_subscription_after_the_revise_interval() => _next.ShouldEqual(Revise);
}
