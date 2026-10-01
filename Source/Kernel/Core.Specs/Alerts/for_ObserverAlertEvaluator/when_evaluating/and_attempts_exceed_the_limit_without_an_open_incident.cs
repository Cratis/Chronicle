// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Alerts;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Alerts.for_ObserverAlertEvaluator.when_evaluating;

public class and_attempts_exceed_the_limit_without_an_open_incident : given.an_evaluator
{
    ObserverAlertEvaluation _result;

    void Because() => _result = Evaluate(SnapshotOf(FailedPartition(FailedPartitionId.New(), TimeSpan.FromMinutes(1), attemptCount: 11)));

    [Fact] void should_raise_exhausted_retries_without_waiting() => _result.Transitions.OfType<AlertRaised>().Single().Condition.ShouldEqual(AlertConditionKind.PartitionRetriesExhausted);
    [Fact] void should_not_schedule_a_raise() => _result.NextRaiseDue.ShouldBeNull();
}
