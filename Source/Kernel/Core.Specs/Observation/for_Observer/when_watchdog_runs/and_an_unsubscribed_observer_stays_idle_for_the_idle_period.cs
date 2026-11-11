// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_Observer.when_watchdog_runs;

public class and_an_unsubscribed_observer_stays_idle_for_the_idle_period : given.an_idle_observer
{
    async Task Because()
    {
        await RunWatchdogAfter(TimeSpan.Zero);
        await RunWatchdogAfter(_idlePeriod);
    }

    [Fact] void should_release_the_activation() => ReleaseRequests.ShouldEqual(1);
}
