// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Alerts;
using Cratis.Chronicle.Observation.for_Observer.given;

namespace Cratis.Chronicle.Observation.for_Observer.when_watchdog_runs;

public class and_observer_has_failed_partitions : an_observer
{
    async Task Establish()
    {
        await _observer.PartitionFailed("partition", 12UL, ["Failed"], "Stack");
        _observerAlerts.ClearReceivedCalls();
    }

    async Task Because() => await _observer.RunWatchdogAsync();

    [Fact] async Task should_report_failures_to_cover_grace_deadlines_and_tracker_loss() => await _observerAlerts.Received(1).Reconcile(Arg.Is<ObserverAlertSnapshot>(_ => _.FailedPartitions.Count == 1));
}
