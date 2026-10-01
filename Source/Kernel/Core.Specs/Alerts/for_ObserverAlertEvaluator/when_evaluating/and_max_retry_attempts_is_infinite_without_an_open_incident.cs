// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Alerts;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Alerts.for_ObserverAlertEvaluator.when_evaluating;

/// <summary>
/// With the maximum number of retries at 0 a failing partition is raised as an ordinary failing partition once the
/// grace period is over, never as retries exhausted.
/// </summary>
public class and_max_retry_attempts_is_infinite_without_an_open_incident : given.an_evaluator
{
    readonly FailedPartitionId _id = FailedPartitionId.New();
    ObserverAlertEvaluation _result;

    void Because() => _result = Evaluate(SnapshotOf(FailedPartition(_id, TimeSpan.FromMinutes(10), isQuarantined: true)) with { MaxRetryAttempts = 0 });

    [Fact] void should_raise_the_partition_failing_condition() => _result.Transitions.OfType<AlertRaised>().Single().Condition.ShouldEqual(AlertConditionKind.PartitionFailing);
}
