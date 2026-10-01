// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Alerts;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Projections;

namespace Cratis.Chronicle.Observation.for_Observer.when_reporting_alert_state;

public class and_the_projection_definition_is_missing_but_failures_remain : for_Observer.given.an_observer
{
    void Establish()
    {
        _definitionStorage.State = _definitionStorage.State with { Type = ObserverType.Projection };
        _eventStoreStorage.Projections.Has((ProjectionId)_observerId.Value).Returns(false);
        _failedPartitionsState.AddFailedPartition("partition", 12UL);
    }

    async Task Because() => await _observer.ReportAlertState();

    [Fact] async Task should_not_skip_the_failed_partition_snapshot() => await _observerAlerts.Received(1).Reconcile(Arg.Is<ObserverAlertSnapshot>(snapshot => snapshot.FailedPartitions.Count == 1));
}
