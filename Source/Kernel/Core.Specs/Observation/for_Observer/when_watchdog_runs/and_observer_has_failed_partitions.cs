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

    [Fact] async Task should_not_repeat_successfully_dispatched_failures() => await _observerAlerts.DidNotReceive().Reconcile(Arg.Any<ObserverAlertSnapshot>());
    [Fact] async Task should_not_poll_for_incidents() => await _observerAlerts.DidNotReceive().HasOpenIncidents();
}
