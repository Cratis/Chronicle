// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Alerts;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Alerts.for_ObserverAlertEvaluator.when_evaluating;

/// <summary>
/// A partition that was escalated and then fails again - another attempt, a new message - is still the same
/// incident. Nothing new is recorded.
/// </summary>
public class and_escalated_partition_fails_again : given.an_evaluator
{
    readonly FailedPartitionId _id = FailedPartitionId.New();
    ObserverAlertEvaluation _result;

    void Because() => _result = Evaluate(
        SnapshotOf(FailedPartition(_id, TimeSpan.FromHours(1), isQuarantined: true, attemptCount: 11, message: "A different failure")),
        OpenPartitionIncident(_id, AlertConditionKind.PartitionRetriesExhausted, AlertSeverity.Critical));

    [Fact] void should_not_transition() => _result.Transitions.ShouldBeEmpty();
    [Fact] void should_not_wait_for_anything() => _result.NextRaiseDue.ShouldBeNull();
}
