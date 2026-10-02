// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Alerts;
using Cratis.Chronicle.Concepts.Alerts;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Projections;

namespace Cratis.Chronicle.Observation.for_Observer.when_watchdog_runs;

public class and_the_last_retained_failure_has_no_projection_definition : for_Observer.given.an_observer
{
    async Task Establish()
    {
        _definitionStorage.State = _definitionStorage.State with { Type = ObserverType.Projection };
        _eventStoreStorage.Projections.Has((ProjectionId)_observerId.Value).Returns(false);
        await _observer.PartitionFailed("partition", 12UL, ["Failed"], "Stack");
        await ReportAlerts();
        await _observer.ClearFailedPartitions();
        _observerAlerts.ClearReceivedCalls();
    }

    async Task Because() => await _observer.RunWatchdogAsync();

    [Fact] async Task should_deliver_the_final_clear_level() => await _observerAlerts.Received(1).Reconcile(Arg.Is<ObserverAlertSnapshot>(_ => _.FailedPartitions.Count == 0 && _.Endings.Values.Contains(AlertClearedReason.Cleared)));
}
