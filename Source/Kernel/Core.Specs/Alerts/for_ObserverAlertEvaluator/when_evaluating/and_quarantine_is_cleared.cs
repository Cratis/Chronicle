// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Alerts;

namespace Cratis.Chronicle.Alerts.for_ObserverAlertEvaluator.when_evaluating;

/// <summary>
/// An operator's ClearObserverQuarantine ends the incident with the supplied Cleared reason.
/// </summary>
public class and_quarantine_is_cleared : given.an_evaluator
{
    readonly IncidentId _id = IncidentId.New();
    ObserverAlertEvaluation _result;

    void Because() => _result = Evaluate(SnapshotOf() with { Endings = new Dictionary<IncidentId, AlertClearedReason> { [_id] = AlertClearedReason.Cleared } }, OpenQuarantineIncident(_id));

    [Fact] void should_only_clear() => _result.Transitions.Count.ShouldEqual(1);
    [Fact] void should_clear_the_incident() => _result.Transitions.OfType<AlertCleared>().Single().IncidentId.ShouldEqual(_id);
    [Fact] void should_clear_it_as_cleared() => _result.Transitions.OfType<AlertCleared>().Single().Reason.ShouldEqual(AlertClearedReason.Cleared);
}
