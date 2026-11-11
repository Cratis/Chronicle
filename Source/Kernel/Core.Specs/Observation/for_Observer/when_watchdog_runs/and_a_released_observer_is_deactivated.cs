// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Alerts;

namespace Cratis.Chronicle.Observation.for_Observer.when_watchdog_runs;

/// <summary>
/// Releasing an idle observer must not leave anything behind that wakes it again, or it would come back every period.
/// </summary>
public class and_a_released_observer_is_deactivated : given.an_idle_observer
{
    async Task Establish()
    {
        await RunWatchdogAfter(TimeSpan.Zero);
        await RunWatchdogAfter(_idlePeriod);
        ResetRecordedWork();
    }

    async Task Because() => await _observer.OnDeactivateAsync(new DeactivationReason(DeactivationReasonCode.ApplicationRequested, "Idle"), default);

    [Fact] void should_have_been_released() => ReleaseRequests.ShouldEqual(1);
    [Fact] void should_not_write_state() => _storageStats.Writes.ShouldEqual(0);
    [Fact] void should_not_register_any_reminder() => ShouldNotHaveRegisteredAnyReminder();
    [Fact] async Task should_not_report_alerts() => await _observerAlerts.DidNotReceive().Reconcile(Arg.Any<ObserverAlertSnapshot>());
}
