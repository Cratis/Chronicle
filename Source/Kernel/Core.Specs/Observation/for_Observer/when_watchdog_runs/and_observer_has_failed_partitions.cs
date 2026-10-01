// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Alerts;
using Cratis.Chronicle.Observation.for_Observer.given;

namespace Cratis.Chronicle.Observation.for_Observer.when_watchdog_runs;

public class and_observer_has_failed_partitions : an_observer
{
    void Establish() => _failedPartitionsState.AddFailedPartition("partition", 12UL);

    async Task Because() => await _observer.RunWatchdogAsync();

    [Fact] async Task should_report_the_current_failures() => await _observerAlerts.Received(1).Reconcile(Arg.Is<ObserverAlertSnapshot>(snapshot => snapshot.FailedPartitions.Count == 1));
}
