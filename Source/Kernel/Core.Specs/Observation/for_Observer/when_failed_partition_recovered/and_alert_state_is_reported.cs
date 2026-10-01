// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Alerts;
using Cratis.Chronicle.Concepts.Alerts;

namespace Cratis.Chronicle.Observation.for_Observer.when_failed_partition_recovered;

public class and_alert_state_is_reported : given.all_dependencies
{
    async Task Because()
    {
        await _observer.FailedPartitionRecovered(_partition, _lastHandledEventSequenceNumber);
        await ReportAlerts();
    }

    [Fact] async Task should_report_the_ended_episode_as_recovered() => await _observerAlerts.Received(1).Reconcile(Arg.Is<ObserverAlertSnapshot>(snapshot => snapshot.Observer == _observerKey && snapshot.FailedPartitions.Count == 0 && snapshot.Endings.Values.Contains(AlertClearedReason.Recovered)));
}
