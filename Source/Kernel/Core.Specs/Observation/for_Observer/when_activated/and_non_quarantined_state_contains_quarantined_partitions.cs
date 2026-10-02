// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Alerts;

namespace Cratis.Chronicle.Observation.for_Observer.when_activated;

public class and_non_quarantined_state_contains_quarantined_partitions : given.an_observer
{
    void Establish()
    {
        _failedPartitionsState.AddFailedPartition("partition", 12UL);
        _failedPartitionsState.Quarantine("partition");
    }

    async Task Because()
    {
        await Crash();
        await _silo.TimerRegistry.FireAllAsync();
    }

    [Fact] async Task should_report_the_restored_partition() => await _observerAlerts.Received().Reconcile(Arg.Is<ObserverAlertSnapshot>(_ => !_.IsQuarantined && _.FailedPartitions.Single().IsQuarantined));
}
