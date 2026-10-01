// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Alerts;

namespace Cratis.Chronicle.Observation.for_Observer.when_watchdog_runs;

public class and_incidents_remain_open_without_failures : for_Observer.given.an_observer
{
    void Establish() => _observerAlerts.HasOpenIncidents().Returns(true);

    async Task Because() => await _observer.RunWatchdogAsync();

    [Fact] async Task should_reconcile_to_clear_the_incidents() => await _observerAlerts.Received(1).Reconcile(Arg.Is<ObserverAlertSnapshot>(snapshot => snapshot.FailedPartitions.Count == 0 && !snapshot.IsQuarantined));
}
