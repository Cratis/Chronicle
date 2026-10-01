// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Alerts;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Projections;

namespace Cratis.Chronicle.Observation.for_Observer.when_reporting_alert_state;

public class and_the_projection_definition_does_not_exist : given.an_observer
{
    void Establish()
    {
        _definitionStorage.State = _definitionStorage.State with { Type = ObserverType.Projection };
        _eventStoreStorage.Projections.Has((ProjectionId)_observerId.Value).Returns(false);
        _eventStoreStorage.Projections.ClearReceivedCalls();
    }

    async Task Because()
    {
        await _observer.ReportAlertState();
        await _observer.ReportAlertState();
        await _observer.RunWatchdogAsync();
    }

    [Fact] async Task should_check_the_definition_only_once_per_activation() => await _eventStoreStorage.Projections.Received(1).Has((ProjectionId)_observerId.Value);
    [Fact] async Task should_not_report_retained_state_without_a_definition() => await _observerAlerts.DidNotReceive().Reconcile(Arg.Any<ObserverAlertSnapshot>());
}
