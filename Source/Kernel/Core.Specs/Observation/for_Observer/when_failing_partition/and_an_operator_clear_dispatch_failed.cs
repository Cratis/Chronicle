// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Alerts;
using Cratis.Chronicle.Concepts.Alerts;

namespace Cratis.Chronicle.Observation.for_Observer.when_failing_partition;

public class and_an_operator_clear_dispatch_failed : given.an_observer
{
    async Task Establish()
    {
        _failedPartitionsState.AddFailedPartition("old", 12UL);
        FailAlertReports(new InvalidOperationException("Dispatch failed"));
        await _observer.ClearFailedPartitions();
        await ReportAlerts();
        ApplyAlertReports();
        _observerAlerts.ClearReceivedCalls();
    }

    async Task Because()
    {
        await _observer.PartitionFailed("new", 42UL, ["Failed"], "Stack");
        await ReportAlerts();
    }

    [Fact] async Task should_preserve_the_operator_clear_reason() => await _observerAlerts.Received(1).Reconcile(Arg.Is<ObserverAlertSnapshot>(snapshot => snapshot.Endings.Values.Contains(AlertClearedReason.Cleared) && snapshot.FailedPartitions.Single().Partition == "new"));
}
