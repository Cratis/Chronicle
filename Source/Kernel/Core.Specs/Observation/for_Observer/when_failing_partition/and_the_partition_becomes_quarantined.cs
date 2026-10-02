// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Alerts;
using Cratis.Chronicle.Configuration;

namespace Cratis.Chronicle.Observation.for_Observer.when_failing_partition;

public class and_the_partition_becomes_quarantined : given.an_observer
{
    protected override Observers CreateObserversConfig() => new() { MaxRetryAttempts = 1 };

    async Task Establish()
    {
        await _observer.PartitionFailed("partition", 42UL, ["Failed"], "Stack");
        _observerAlerts.ClearReceivedCalls();
    }

    async Task Because()
    {
        await _observer.PartitionFailed("partition", 42UL, ["Failed again"], "Stack");
        await ReportAlerts();
    }

    [Fact] async Task should_report_the_new_partition_quarantine() => await _observerAlerts.Received(1).Reconcile(Arg.Is<ObserverAlertSnapshot>(snapshot => snapshot.FailedPartitions.Single().IsQuarantined && snapshot.FailedPartitions.Single().AttemptCount == 2));
}
