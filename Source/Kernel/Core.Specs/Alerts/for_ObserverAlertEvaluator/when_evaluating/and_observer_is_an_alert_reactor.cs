// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Alerts.for_ObserverAlertEvaluator.when_evaluating;

/// <summary>
/// The kernel's own alert observers are never alerted about, so an alert observer that fails cannot raise an alert
/// about itself.
/// </summary>
public class and_observer_is_an_alert_reactor : given.an_evaluator
{
    ObserverAlertEvaluation _result;

    void Because() => _result = Evaluate(
        SnapshotOf(FailedPartition(FailedPartitionId.New(), TimeSpan.FromHours(1), isQuarantined: true)) with
        {
            Observer = _observer with { ObserverId = $"{ObserverAlertEvaluator.AlertObserverPrefix}AlertIncidentsReactor" },
            IsQuarantined = true
        });

    [Fact] void should_not_transition() => _result.Transitions.ShouldBeEmpty();
    [Fact] void should_not_wait_for_anything() => _result.NextRaiseDue.ShouldBeNull();
}
