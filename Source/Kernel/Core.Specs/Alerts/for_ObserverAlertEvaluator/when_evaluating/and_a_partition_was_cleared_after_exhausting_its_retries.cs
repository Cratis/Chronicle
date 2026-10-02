// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Alerts.for_ObserverAlertEvaluator.when_evaluating;

/// <summary>
/// A partition whose quarantine was cleared keeps its attempt history, but its retry budget starts over, so it must
/// not keep counting as exhausted on the strength of attempts made before the reset.
/// </summary>
public class and_a_partition_was_cleared_after_exhausting_its_retries : given.an_evaluator
{
    readonly FailedPartitionId _id = FailedPartitionId.New();
    ObserverAlertEvaluation _result;

    void Because() => _result = Evaluate(
        SnapshotOf(FailedPartition(_id, TimeSpan.FromHours(1), attemptCount: 12, attemptsInCurrentBudget: 1)),
        OpenPartitionIncident(_id));

    [Fact] void should_not_escalate_on_the_strength_of_attempts_before_the_reset() => _result.Transitions.OfType<AlertEscalated>().ShouldBeEmpty();
}
