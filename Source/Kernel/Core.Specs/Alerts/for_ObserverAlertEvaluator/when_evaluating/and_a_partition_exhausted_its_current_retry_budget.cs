// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Alerts.for_ObserverAlertEvaluator.when_evaluating;

/// <summary>
/// The counterpart of a cleared partition: attempts within the current retry budget beyond the maximum escalate.
/// </summary>
public class and_a_partition_exhausted_its_current_retry_budget : given.an_evaluator
{
    readonly FailedPartitionId _id = FailedPartitionId.New();
    ObserverAlertEvaluation _result;

    void Because() => _result = Evaluate(
        SnapshotOf(FailedPartition(_id, TimeSpan.FromHours(1), attemptCount: 12, attemptsInCurrentBudget: 11)),
        OpenPartitionIncident(_id));

    [Fact] void should_escalate() => _result.Transitions.OfType<AlertEscalated>().Count().ShouldEqual(1);
}
