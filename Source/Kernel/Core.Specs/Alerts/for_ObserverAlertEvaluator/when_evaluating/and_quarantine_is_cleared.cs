// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Alerts;

namespace Cratis.Chronicle.Alerts.for_ObserverAlertEvaluator.when_evaluating;

/// <summary>
/// When the snapshot does not say how the quarantine ended, the incident clears as cleared.
/// </summary>
public class and_quarantine_is_cleared : given.an_evaluator
{
    ObserverAlertEvaluation _result;

    void Because() => _result = Evaluate(SnapshotOf(), OpenQuarantineIncident());

    [Fact] void should_clear_it_as_cleared() => _result.Transitions.OfType<AlertCleared>().Single().Reason.ShouldEqual(AlertClearedReason.Cleared);
}
