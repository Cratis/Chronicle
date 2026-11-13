// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_Observer.when_watchdog_runs;

public class and_a_subscribed_observer_stays_idle_for_the_idle_period : given.an_idle_observer
{
    void Establish() => _observer.SetSubscription(ASubscription());

    async Task Because()
    {
        await RunWatchdogAfter(TimeSpan.Zero);
        await RunWatchdogAfter(_idlePeriod * 2);
    }

    [Fact] void should_keep_the_activation_for_its_live_subscription() => ReleaseRequests.ShouldEqual(0);
}
