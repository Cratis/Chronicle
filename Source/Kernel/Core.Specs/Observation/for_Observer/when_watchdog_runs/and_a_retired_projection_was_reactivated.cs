// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Alerts;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Observation.for_Observer.given;
using Cratis.Chronicle.Observation.States;

namespace Cratis.Chronicle.Observation.for_Observer.when_watchdog_runs;

public class and_a_retired_projection_was_reactivated : an_observer_with_subscription
{
    async Task Establish()
    {
        _definitionStorage.State = _definitionStorage.State with { Type = ObserverType.Projection };
        _eventStoreStorage.Projections.Has((ProjectionId)_observerId.Value).Returns(true);
        _failedPartitionsState.AddFailedPartition("partition", 12UL);
        await _observer.TransitionTo<QuarantinedObserver>();
        await _observer.Retire();

        // Projection.Remove deletes this definition; the observer's definition and quarantine remain.
        _eventStoreStorage.Projections.Has((ProjectionId)_observerId.Value).Returns(false);
        _observerAlerts.ClearReceivedCalls();
        await Reactivate();
    }

    async Task Because() => await _observer.RunWatchdogAsync();

    [Fact] async Task should_keep_the_quarantined_state() => (await _observer.IsObserverQuarantined()).ShouldBeTrue();
    [Fact] async Task should_have_no_failed_partitions() => (await _observer.HasFailedPartitions()).ShouldBeFalse();
    [Fact] async Task should_report_retirement_instead_of_reopening_quarantine() => await _observerAlerts.Received(1).Reconcile(Arg.Is<ObserverAlertSnapshot>(_ => _.Disposition == AlertDisposition.Retired));
}
