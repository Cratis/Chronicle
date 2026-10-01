// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Alerts;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Observation.for_Observer.given;
using Cratis.Chronicle.Observation.States;

namespace Cratis.Chronicle.Observation.for_Observer.when_watchdog_runs;

public class and_a_registered_quarantined_projection_was_reactivated : an_observer_with_subscription
{
    async Task Establish()
    {
        _definitionStorage.State = _definitionStorage.State with { Type = ObserverType.Projection };
        _eventStoreStorage.Projections.Has((ProjectionId)_observerId.Value).Returns(true);
        await _observer.TransitionTo<QuarantinedObserver>();
        await Reactivate();
        _observerAlerts.ClearReceivedCalls();
    }

    async Task Because() => await _observer.RunWatchdogAsync();

    [Fact] async Task should_not_repeat_successfully_dispatched_quarantine() => await _observerAlerts.DidNotReceive().Reconcile(Arg.Any<ObserverAlertSnapshot>());
    [Fact] async Task should_not_poll_the_tracker() => await _observerAlerts.DidNotReceive().HasOpenIncidents();
}
