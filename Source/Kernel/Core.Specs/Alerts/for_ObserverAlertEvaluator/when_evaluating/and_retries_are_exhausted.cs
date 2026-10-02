// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Alerts;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Alerts.for_ObserverAlertEvaluator.when_evaluating;

/// <summary>
/// When a partition runs out of retries the incident that is already open is escalated, not replaced: it keeps its
/// identifier and moves to the retries exhausted condition as critical.
/// </summary>
public class and_retries_are_exhausted : given.an_evaluator
{
    readonly FailedPartitionId _id = FailedPartitionId.New();
    ObserverAlertEvaluation _result;
    AlertEscalated _escalated;

    void Because()
    {
        _result = Evaluate(SnapshotOf(FailedPartition(_id, TimeSpan.FromMinutes(30), isQuarantined: true, attemptCount: 10)), OpenPartitionIncident(_id));
        _escalated = _result.Transitions.OfType<AlertEscalated>().Single();
    }

    [Fact] void should_only_escalate() => _result.Transitions.Count.ShouldEqual(1);
    [Fact] void should_keep_the_incident_id() => _escalated.IncidentId.ShouldEqual((IncidentId)_id);
    [Fact] void should_move_to_the_retries_exhausted_condition() => _escalated.Condition.ShouldEqual(AlertConditionKind.PartitionRetriesExhausted);
    [Fact] void should_be_critical() => _escalated.Severity.ShouldEqual(AlertSeverity.Critical);
    [Fact] void should_carry_the_attempt_count() => _escalated.Evidence.AttemptCount.ShouldEqual(10);
    [Fact] void should_target_the_partition() => _escalated.Target.Partition.ShouldEqual((AlertPartition)"partition");
}
