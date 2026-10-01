// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Alerts;
using Cratis.Chronicle.Concepts.Alerts;

namespace Cratis.Chronicle.Observation.for_Observer.when_watchdog_runs;

public class and_a_clear_report_failed : for_Observer.given.an_observer
{
    async Task Establish()
    {
        _failedPartitionsState.AddFailedPartition("partition", 12UL);
        FailAlertReports(new InvalidOperationException("Dispatch failed"));
        await _observer.ClearFailedPartitions();
        await ReportAlerts();
        ApplyAlertReports();
        _observerAlerts.ClearReceivedCalls();
    }

    async Task Because()
    {
        await _observer.RunWatchdogAsync();
        await _observer.RunWatchdogAsync();
    }

    [Fact] async Task should_retry_the_clear_once_with_its_original_reason() => await _observerAlerts.Received(1).Reconcile(Arg.Is<ObserverAlertSnapshot>(snapshot => snapshot.FailedPartitions.Count == 0 && snapshot.Endings.Values.Contains(AlertClearedReason.Cleared)));
}
