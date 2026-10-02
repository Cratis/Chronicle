// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Alerts;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Alerts.for_ObserverAlertEvaluator.when_evaluating;

public class and_retired_state_retains_operational_quarantine : given.an_evaluator
{
    ObserverAlertEvaluation _result;

    void Because() => _result = Evaluate(SnapshotOf() with { IsQuarantined = true, Disposition = AlertDisposition.Retired }, OpenQuarantineIncident());

    [Fact] void should_clear_as_removed() => ((AlertCleared)_result.Transitions.Single()).Reason.ShouldEqual(AlertClearedReason.Removed);
    [Fact] void should_not_reopen_retained_quarantine() => _result.Transitions.OfType<AlertRaised>().ShouldBeEmpty();
}
