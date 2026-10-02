// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Configuration;

namespace Cratis.Chronicle.Alerts.for_ObserverAlertEvaluator.when_evaluating;

/// <summary>
/// A disabled condition raises nothing, however long the partition has been failing.
/// </summary>
public class and_condition_is_disabled : given.an_evaluator
{
    ObserverAlertEvaluation _result;

    void Establish() => Configure(AlertsWith("partition-failing", new AlertConditionOptions { Enabled = false }));

    void Because() => _result = Evaluate(SnapshotOf(FailedPartition(FailedPartitionId.New(), TimeSpan.FromHours(1))));

    [Fact] void should_not_transition() => _result.Transitions.ShouldBeEmpty();
    [Fact] void should_not_wait_for_anything() => _result.NextRaiseDue.ShouldBeNull();
}
