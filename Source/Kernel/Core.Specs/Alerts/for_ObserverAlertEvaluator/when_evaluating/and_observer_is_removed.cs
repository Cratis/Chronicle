// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Alerts;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Alerts.for_ObserverAlertEvaluator.when_evaluating;

/// <summary>
/// A removed observer raises nothing and clears every incident it has open as removed - its partition incidents and
/// its quarantine incident alike.
/// </summary>
public class and_observer_is_removed : given.an_evaluator
{
    readonly FailedPartitionId _id = FailedPartitionId.New();
    readonly IncidentId _quarantineId = IncidentId.New();
    ObserverAlertEvaluation _result;

    void Because() => _result = Evaluate(
        SnapshotOf(FailedPartition(FailedPartitionId.New(), TimeSpan.FromHours(1))) with { IsQuarantined = true, Disposition = AlertDisposition.Retired },
        OpenPartitionIncident(_id),
        OpenQuarantineIncident(_quarantineId));

    [Fact] void should_only_clear() => _result.Transitions.OfType<AlertCleared>().Count().ShouldEqual(_result.Transitions.Count);
    [Fact] void should_clear_both_incidents() => _result.Transitions.OfType<AlertCleared>().Select(_ => _.IncidentId).ShouldContainOnly((IncidentId)_id, _quarantineId);
    [Fact] void should_clear_them_as_removed() => _result.Transitions.OfType<AlertCleared>().All(_ => _.Reason == AlertClearedReason.Removed).ShouldBeTrue();
    [Fact] void should_not_wait_for_anything() => _result.NextRaiseDue.ShouldBeNull();
}
