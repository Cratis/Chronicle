// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Alerts;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Configuration;

namespace Cratis.Chronicle.Alerts.for_ObserverAlertEvaluator.when_evaluating;

public class and_raise_after_is_zero : given.an_evaluator
{
    ObserverAlertEvaluation _result;

    void Establish() => Configure(AlertsWith("partition-failing", new AlertConditionOptions { RaiseAfter = TimeSpan.Zero }));

    void Because() => _result = Evaluate(SnapshotOf(FailedPartition(FailedPartitionId.New(), TimeSpan.Zero)));

    [Fact] void should_raise_immediately() => _result.Transitions.OfType<AlertRaised>().Single().Condition.ShouldEqual(AlertConditionKind.PartitionFailing);
    [Fact] void should_not_wait_for_a_grace_period() => _result.NextRaiseDue.ShouldBeNull();
}
