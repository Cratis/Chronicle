// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Alerts;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Observation.States;

namespace Cratis.Chronicle.Observation.for_Observer.when_activated;

public class and_retirement_completed_before_definition_cleanup : given.an_observer
{
    async Task Establish()
    {
        _definitionStorage.State = _definitionStorage.State with { Type = ObserverType.Projection };
        _eventStoreStorage.Projections.Has((ProjectionId)_observerId.Value).Returns(true);
        await _observer.TransitionTo<QuarantinedObserver>();
        await _observer.Retire();
        _observerAlerts.ClearReceivedCalls();
    }

    async Task Because()
    {
        await Crash();
        await ReportAlerts();
    }

    [Fact] async Task should_report_the_durable_retired_disposition() => await _observerAlerts.Received(1).Reconcile(Arg.Is<ObserverAlertSnapshot>(_ => _.Disposition == AlertDisposition.Retired));
    [Fact] void should_retain_operational_quarantine_without_reopening_it() => _stateStorage.State.RunningState.ShouldEqual(ObserverRunningState.Quarantined);
}
