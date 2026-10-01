// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Alerts;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Alerts.for_ObserverAlertEvaluator.when_evaluating;

/// <summary>
/// A quarantined partition is never retried automatically, so even with the maximum number of retries at 0 it needs a
/// person: the open incident is escalated to the retries exhausted condition.
/// </summary>
public class and_max_retry_attempts_is_infinite_and_partition_is_quarantined : given.an_evaluator
{
    readonly FailedPartitionId _id = FailedPartitionId.New();
    ObserverAlertEvaluation _result;
    AlertEscalated _escalated;

    void Because()
    {
        _result = Evaluate(
            SnapshotOf(FailedPartition(_id, TimeSpan.FromHours(1), isQuarantined: true)) with { MaxRetryAttempts = 0 },
            OpenPartitionIncident(_id));
        _escalated = _result.Transitions.OfType<AlertEscalated>().Single();
    }

    [Fact] void should_only_escalate() => _result.Transitions.Count.ShouldEqual(1);
    [Fact] void should_move_to_the_retries_exhausted_condition() => _escalated.Condition.ShouldEqual(AlertConditionKind.PartitionRetriesExhausted);
    [Fact] void should_be_critical() => _escalated.Severity.ShouldEqual(AlertSeverity.Critical);
}
