// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Alerts.for_ObserverAlertEvaluator.when_evaluating;

/// <summary>
/// An observer that is still quarantined while its incident is open raises nothing more and clears nothing.
/// </summary>
public class and_observer_stays_quarantined : given.an_evaluator
{
    ObserverAlertEvaluation _result;

    void Because() => _result = Evaluate(SnapshotOf() with { IsQuarantined = true }, OpenQuarantineIncident());

    [Fact] void should_not_transition() => _result.Transitions.ShouldBeEmpty();
}
