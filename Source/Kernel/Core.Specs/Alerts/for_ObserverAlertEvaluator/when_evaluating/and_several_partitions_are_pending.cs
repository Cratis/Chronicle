// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Alerts.for_ObserverAlertEvaluator.when_evaluating;

/// <summary>
/// With several partitions waiting for their grace period the evaluator reports the earliest time one of them falls
/// due, so a single timer is enough.
/// </summary>
public class and_several_partitions_are_pending : given.an_evaluator
{
    ObserverAlertEvaluation _result;
    FailedPartitionSnapshot _earliest;

    void Because()
    {
        _earliest = FailedPartition(FailedPartitionId.New(), TimeSpan.FromMinutes(4), partition: "earliest");
        _result = Evaluate(SnapshotOf(
            FailedPartition(FailedPartitionId.New(), TimeSpan.FromMinutes(1), partition: "later"),
            _earliest,
            FailedPartition(FailedPartitionId.New(), TimeSpan.FromMinutes(2), partition: "latest")));
    }

    [Fact] void should_not_transition() => _result.Transitions.ShouldBeEmpty();
    [Fact] void should_report_the_earliest_time() => _result.NextRaiseDue.ShouldEqual(_earliest.FirstAttempt + _graceTime);
}
