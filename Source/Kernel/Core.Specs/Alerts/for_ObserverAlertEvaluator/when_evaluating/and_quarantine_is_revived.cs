// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Alerts;

namespace Cratis.Chronicle.Alerts.for_ObserverAlertEvaluator.when_evaluating;

/// <summary>
/// An observer revived from quarantine by a fresh client subscription clears its incident as revived, when the
/// snapshot says so.
/// </summary>
public class and_quarantine_is_revived : given.an_evaluator
{
    readonly IncidentId _id = IncidentId.New();
    ObserverAlertEvaluation _result;
    AlertCleared _cleared;

    void Because()
    {
        _result = Evaluate(SnapshotOf() with { QuarantineEndedAs = AlertClearedReason.Revived }, OpenQuarantineIncident(_id));
        _cleared = _result.Transitions.OfType<AlertCleared>().Single();
    }

    [Fact] void should_only_clear() => _result.Transitions.Count.ShouldEqual(1);
    [Fact] void should_clear_the_incident() => _cleared.IncidentId.ShouldEqual(_id);
    [Fact] void should_clear_it_as_revived() => _cleared.Reason.ShouldEqual(AlertClearedReason.Revived);
    [Fact] void should_target_the_observer_without_a_partition() => _cleared.Target.ShouldEqual(AlertTarget.For(_observer, AlertPartition.None));
}
