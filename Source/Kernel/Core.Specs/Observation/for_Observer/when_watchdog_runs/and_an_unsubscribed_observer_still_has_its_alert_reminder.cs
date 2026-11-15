// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_Observer.when_watchdog_runs;

public class and_an_unsubscribed_observer_still_has_its_alert_reminder : given.an_idle_observer
{
    void Establish() => _observer.ScheduleAlertReport();

    async Task Because()
    {
        await RunWatchdogAfter(TimeSpan.Zero);
        await RunWatchdogAfter(_idlePeriod * 2);
    }

    [Fact] void should_keep_the_activation_until_the_reminder_is_given_up() => ReleaseRequests.ShouldEqual(0);
}
