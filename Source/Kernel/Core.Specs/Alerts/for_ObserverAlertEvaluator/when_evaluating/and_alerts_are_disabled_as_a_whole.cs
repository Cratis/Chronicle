// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

using AlertsOptions = Cratis.Chronicle.Configuration.Alerts;

namespace Cratis.Chronicle.Alerts.for_ObserverAlertEvaluator.when_evaluating;

/// <summary>
/// Turning alerts off as a whole stops every condition from raising, escalating or reporting a raise to come.
/// </summary>
public class and_alerts_are_disabled_as_a_whole : given.an_evaluator
{
    ObserverAlertEvaluation _result;

    void Establish() => Configure(new AlertsOptions { Enabled = false });

    void Because() => _result = Evaluate(SnapshotOf(FailedPartition(FailedPartitionId.New(), TimeSpan.FromMinutes(1))) with { IsQuarantined = true });

    [Fact] void should_not_transition() => _result.Transitions.ShouldBeEmpty();
    [Fact] void should_not_wait_for_anything() => _result.NextRaiseDue.ShouldBeNull();
}
