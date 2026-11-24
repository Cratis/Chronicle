// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Alerts;

namespace Cratis.Chronicle.Observation.for_Observer.when_activated;

/// <summary>
/// An observer that wakes with a quarantined partition still reports it, because the loaded state is alert-relevant.
/// </summary>
public class and_a_partition_has_failed : given.a_quarantined_partition
{
    async Task Because()
    {
        await Crash();
        await _silo.TimerRegistry.FireAllAsync();
    }

    [Fact] async Task should_report_the_failed_partition() => await _observerAlerts.Received().Reconcile(Arg.Is<ObserverAlertSnapshot>(_ => _.FailedPartitions.Count == 1));
}
