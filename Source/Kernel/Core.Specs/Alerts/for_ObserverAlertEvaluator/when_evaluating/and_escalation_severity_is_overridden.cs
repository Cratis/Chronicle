// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Alerts;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Configuration;

namespace Cratis.Chronicle.Alerts.for_ObserverAlertEvaluator.when_evaluating;

/// <summary>
/// The severity an incident is escalated to comes from the configuration of the retries exhausted condition.
/// </summary>
public class and_escalation_severity_is_overridden : given.an_evaluator
{
    readonly FailedPartitionId _id = FailedPartitionId.New();
    ObserverAlertEvaluation _result;

    void Establish() => Configure(AlertsWith("partition-retries-exhausted", new AlertConditionOptions { Severity = AlertSeverity.Warning }));

    void Because() => _result = Evaluate(SnapshotOf(FailedPartition(_id, TimeSpan.FromHours(1), isQuarantined: true)), OpenPartitionIncident(_id));

    [Fact] void should_escalate_with_the_configured_severity() => _result.Transitions.OfType<AlertEscalated>().Single().Severity.ShouldEqual(AlertSeverity.Warning);
}
