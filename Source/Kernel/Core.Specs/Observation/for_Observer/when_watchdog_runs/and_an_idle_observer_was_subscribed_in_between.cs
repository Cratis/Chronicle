// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_Observer.when_watchdog_runs;

public class and_an_idle_observer_was_subscribed_in_between : given.an_idle_observer
{
    async Task Because()
    {
        await RunWatchdogAfter(TimeSpan.Zero);
        _observer.SetSubscription(ASubscription());
        await RunWatchdogAfter(_idlePeriod / 2);
        _observer.SetSubscription(ObserverSubscription.Unsubscribed);
        await RunWatchdogAfter(_idlePeriod / 2);
        await RunWatchdogAfter(_idlePeriod / 2);
    }

    [Fact] void should_start_the_idle_period_over() => _observer.IsReleasedWhenIdle.ShouldBeFalse();
}
