// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Alerts;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.when_failing_partition;

/// <summary>
/// A healthy activation reports nothing, so the first failure is what reaches the alert companion, and it does so once.
/// </summary>
public class and_the_observer_was_healthy_on_activation : given.an_observer
{
    async Task Because()
    {
        await Crash();
        await _silo.TimerRegistry.FireAllAsync();
        _observerAlerts.ClearReceivedCalls();
        await _observer.PartitionFailed("partition", 42UL, ["Failed"], "Stack", FailureKind.Handling);
        await _silo.TimerRegistry.FireAllAsync();
    }

    [Fact] async Task should_report_the_failure_once() => await _observerAlerts.Received(1).Reconcile(Arg.Is<ObserverAlertSnapshot>(_ => _.FailedPartitions.Count == 1));
}
