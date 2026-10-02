// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Alerts;

namespace Cratis.Chronicle.Alerts.for_ObserverAlertEvaluator.when_evaluating;

public class and_an_unknown_condition_is_open : given.an_evaluator
{
    ObserverAlertEvaluation _result;

    void Because() => _result = Evaluate(SnapshotOf(), new OpenIncident(IncidentId.New(), "observer-stalled", AlertSeverity.Warning, AlertPartition.None));

    [Fact] void should_leave_an_incident_it_cannot_evaluate_open() => _result.Transitions.ShouldBeEmpty();
}
