// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_Observer.when_watchdog_runs;

/// <summary>
/// Orleans deactivates a released observer before it handles another call, so this should not happen. Should it, the
/// subscription keeps the activation and its watchdog runs again.
/// </summary>
public class and_a_released_observer_is_subscribed_before_it_is_deactivated : given.an_idle_observer
{
    async Task Establish()
    {
        await RunWatchdogAfter(TimeSpan.Zero);
        await RunWatchdogAfter(_idlePeriod);
        _observer.SetSubscription(ASubscription());
        ResetRecordedWork();
    }

    async Task Because()
    {
        await RunWatchdogAfter(TimeSpan.FromSeconds(1));
        _observer.SetSubscription(ObserverSubscription.Unsubscribed);
        await RunWatchdogAfter(_idlePeriod / 2);
    }

    [Fact] void should_not_ask_to_be_released_again() => ReleaseRequests.ShouldEqual(1);
    [Fact] void should_run_the_watchdog_checks_again() => _definitionStorageStats.Reads.ShouldBeGreaterThan(0);
}
