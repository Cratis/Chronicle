// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Alerts.for_ObserverAlertEvaluator.when_evaluating;

/// <summary>
/// A partition that recovers within the grace period must raise nothing, so a failure is not announced until it has
/// lasted. The evaluator reports when the raise falls due instead, so the caller can come back for it.
/// </summary>
public class and_partition_failed_within_raise_after : given.an_evaluator
{
    readonly FailedPartitionId _id = FailedPartitionId.New();
    ObserverAlertEvaluation _result;
    FailedPartitionSnapshot _partition;

    void Because()
    {
        _partition = FailedPartition(_id, TimeSpan.FromMinutes(2));
        _result = Evaluate(SnapshotOf(_partition));
    }

    [Fact] void should_not_transition() => _result.Transitions.ShouldBeEmpty();
    [Fact] void should_report_when_the_raise_falls_due() => _result.NextRaiseDue.ShouldEqual(_partition.FirstAttempt + _graceTime);
}
