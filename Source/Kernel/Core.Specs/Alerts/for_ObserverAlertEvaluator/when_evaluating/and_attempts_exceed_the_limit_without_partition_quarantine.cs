// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Alerts;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Alerts.for_ObserverAlertEvaluator.when_evaluating;

/// <summary>
/// Observer-wide quarantine can bypass setting the partition flag. Startup recovery still refuses attempts above the limit.
/// </summary>
public class and_attempts_exceed_the_limit_without_partition_quarantine : given.an_evaluator
{
    readonly FailedPartitionId _id = FailedPartitionId.New();
    ObserverAlertEvaluation _result;

    void Because() => _result = Evaluate(
        SnapshotOf(FailedPartition(_id, TimeSpan.FromHours(1), attemptCount: 11)),
        OpenPartitionIncident(_id));

    [Fact] void should_escalate_the_same_incident() => _result.Transitions.OfType<AlertEscalated>().Single().IncidentId.ShouldEqual((IncidentId)_id);
    [Fact] void should_report_exhausted_retries() => _result.Transitions.OfType<AlertEscalated>().Single().Condition.ShouldEqual(AlertConditionKind.PartitionRetriesExhausted);
}
