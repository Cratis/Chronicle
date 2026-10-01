// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Alerts;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Configuration;

namespace Cratis.Chronicle.Alerts.for_ObserverAlertEvaluator.when_evaluating;

public class and_an_unknown_condition_is_configured : given.an_evaluator
{
    ObserverAlertEvaluation _result;

    void Establish() => Configure(AlertsWith("observer-stalled", new AlertConditionOptions { Severity = AlertSeverity.Critical }));

    void Because() => _result = Evaluate(SnapshotOf(FailedPartition(FailedPartitionId.New(), TimeSpan.FromMinutes(10))));

    [Fact] void should_evaluate_only_the_supported_conditions() => _result.Transitions.OfType<AlertRaised>().Single().Condition.ShouldEqual(AlertConditionKind.PartitionFailing);
    [Fact] void should_keep_the_supported_default() => _result.Transitions.OfType<AlertRaised>().Single().Severity.ShouldEqual(AlertSeverity.Warning);
}
