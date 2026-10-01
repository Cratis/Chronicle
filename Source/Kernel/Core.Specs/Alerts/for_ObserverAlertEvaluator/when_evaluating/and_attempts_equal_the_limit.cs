// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Alerts.for_ObserverAlertEvaluator.when_evaluating;

public class and_attempts_equal_the_limit : given.an_evaluator
{
    readonly FailedPartitionId _id = FailedPartitionId.New();
    ObserverAlertEvaluation _result;

    void Because() => _result = Evaluate(SnapshotOf(FailedPartition(_id, TimeSpan.FromHours(1), attemptCount: 10)), OpenPartitionIncident(_id));

    [Fact] void should_not_escalate_while_one_more_retry_is_allowed() => _result.Transitions.ShouldBeEmpty();
}
