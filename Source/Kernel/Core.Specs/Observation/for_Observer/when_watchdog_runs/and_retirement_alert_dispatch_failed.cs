// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Alerts;

namespace Cratis.Chronicle.Observation.for_Observer.when_watchdog_runs;

public class and_retirement_alert_dispatch_failed : for_Observer.given.a_reactivated_quarantined_observer
{
    async Task Establish()
    {
        _observerAlerts.Removed().Returns(Task.FromException(new InvalidOperationException("Dispatch failed")));
        await _observer.Retire();
        _observerAlerts.Removed().Returns(Task.CompletedTask);
        _observerAlerts.ClearReceivedCalls();
    }

    async Task Because()
    {
        await _observer.RunWatchdogAsync();
        await _observer.RunWatchdogAsync();
    }

    [Fact] async Task should_retry_removed_only_once() => await _observerAlerts.Received(1).Removed();
    [Fact] async Task should_not_report_retained_quarantine() => await _observerAlerts.DidNotReceive().Reconcile(Arg.Any<ObserverAlertSnapshot>());
    [Fact] async Task should_keep_the_observer_quarantined() => (await _observer.IsObserverQuarantined()).ShouldBeTrue();
}
