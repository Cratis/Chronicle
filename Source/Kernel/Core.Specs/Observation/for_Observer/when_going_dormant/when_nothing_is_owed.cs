// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_Observer.when_going_dormant;

public class when_nothing_is_owed : given.a_subscribed_client_observer
{
    async Task Because()
    {
        await RunWatchdogAfter(TimeSpan.Zero);
        await RunWatchdogAfter(_dormantPeriod);
    }

    [Fact] void should_release_the_activation() => ReleaseRequests.ShouldEqual(1);
    [Fact] void should_have_recorded_the_subscription() => Recorded.ShouldNotBeNull();
}
