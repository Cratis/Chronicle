// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Alerts;

namespace Cratis.Chronicle.Observation.for_Observer.when_watchdog_runs;

public class and_removal_alert_dispatch_failed : for_Observer.given.an_observer
{
    async Task Establish()
    {
        _observerAlerts.Removed().Returns(Task.FromException(new InvalidOperationException("Dispatch failed")));
        await _observer.Remove();
        _observerAlerts.Removed().Returns(Task.CompletedTask);
        _observerAlerts.ClearReceivedCalls();
        _storageStats.ResetCounts();
    }

    async Task Because()
    {
        await _observer.RunWatchdogAsync();
        await _observer.RunWatchdogAsync();
    }

    [Fact] async Task should_retry_the_removal_dispatch_once() => await _observerAlerts.Received(1).Removed();
    [Fact] async Task should_not_report_observer_state() => await _observerAlerts.DidNotReceive().Reconcile(Arg.Any<ObserverAlertSnapshot>());
    [Fact] void should_not_recreate_removed_state() => _storageStats.Writes.ShouldEqual(0);
}
