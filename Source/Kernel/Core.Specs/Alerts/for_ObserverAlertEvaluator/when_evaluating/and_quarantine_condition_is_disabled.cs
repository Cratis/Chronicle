// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;

namespace Cratis.Chronicle.Alerts.for_ObserverAlertEvaluator.when_evaluating;

/// <summary>
/// A disabled observer quarantined condition raises nothing for a quarantined observer.
/// </summary>
public class and_quarantine_condition_is_disabled : given.an_evaluator
{
    ObserverAlertEvaluation _result;

    void Establish() => Configure(AlertsWith("observer-quarantined", new AlertConditionOptions { Enabled = false }));

    void Because() => _result = Evaluate(SnapshotOf() with { IsQuarantined = true });

    [Fact] void should_not_transition() => _result.Transitions.ShouldBeEmpty();
}
