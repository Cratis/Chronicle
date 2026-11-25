// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_Observer.when_going_dormant;

public class when_the_dormant_period_has_not_passed : given.a_subscribed_client_observer
{
    async Task Because()
    {
        await RunWatchdogAfter(TimeSpan.Zero);
        await RunWatchdogAfter(_dormantPeriod - TimeSpan.FromSeconds(1));
    }

    [Fact] void should_keep_the_activation() => ReleaseRequests.ShouldEqual(0);
}
