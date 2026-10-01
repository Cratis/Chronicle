// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Alerts;
using Cratis.Chronicle.Concepts.Alerts;

namespace Cratis.Chronicle.Observation.for_Observer;

public class when_clearing_failed_partitions : given.an_observer
{
    void Establish() => _failedPartitionsState.AddFailedPartition("partition", 12UL);

    async Task Because()
    {
        await _observer.ClearFailedPartitions();
        await ReportAlerts();
    }

    [Fact] async Task should_report_the_ended_episodes_as_cleared() => await _observerAlerts.Received(1).Reconcile(Arg.Is<ObserverAlertSnapshot>(snapshot => snapshot.FailedPartitions.Count == 0 && snapshot.Endings.Values.Contains(AlertClearedReason.Cleared)));
}
