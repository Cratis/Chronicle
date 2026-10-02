// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Alerts;
using Cratis.Chronicle.Observation.Alerts;

namespace Cratis.Chronicle.Observation.for_Observer.when_watchdog_runs;

public class and_incidents_remain_open_without_failures : for_Observer.given.an_observer
{
    async Task Establish()
    {
        await Crash();
        _observerAlerts.Reconcile(Arg.Any<ObserverAlertSnapshot>()).Returns(call =>
        {
            var report = call.Arg<ObserverAlertSnapshot>();
            return new ObserverAlertReceipt(report.LifecycleId, report.Revision, ObserverAlertReconciliation.RetryRequired);
        });
        await ReportAlerts();
        ApplyAlertReports();
        _observerAlerts.ClearReceivedCalls();
    }

    async Task Because()
    {
        await _observer.RunWatchdogAsync();
        await _observer.RunWatchdogAsync();
    }

    [Fact] async Task should_retry_the_unacknowledged_empty_level_once() => await _observerAlerts.Received(1).Reconcile(Arg.Is<ObserverAlertSnapshot>(_ => _.FailedPartitions.Count == 0));
}
