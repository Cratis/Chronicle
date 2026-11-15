// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Alerts;

namespace Cratis.Chronicle.Observation.for_Observer.when_watchdog_runs;

public class and_a_released_observer_ticks_again : given.an_idle_observer
{
    async Task Establish()
    {
        await RunWatchdogAfter(TimeSpan.Zero);
        await RunWatchdogAfter(_idlePeriod);
        ResetRecordedWork();
    }

    async Task Because() => await RunWatchdogAfter(TimeSpan.FromSeconds(1));

    [Fact] void should_only_ask_to_be_released_again() => ReleaseRequests.ShouldEqual(2);
    [Fact] void should_not_run_the_watchdog_checks() => _definitionStorageStats.Reads.ShouldEqual(0);
    [Fact] void should_not_write_state() => _storageStats.Writes.ShouldEqual(0);
    [Fact] async Task should_not_report_alerts() => await _observerAlerts.DidNotReceive().Reconcile(Arg.Any<ObserverAlertSnapshot>());
    [Fact] void should_not_register_any_reminder() => ShouldNotHaveRegisteredAnyReminder();
}
