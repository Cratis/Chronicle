// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Configuration;

namespace Cratis.Chronicle.Alerts.for_ObserverAlertEvaluator.when_evaluating;

/// <summary>
/// How long a partition has to keep failing before it raises comes from the configuration.
/// </summary>
public class and_raise_after_is_configured : given.an_evaluator
{
    readonly FailedPartitionId _id = FailedPartitionId.New();
    ObserverAlertEvaluation _result;
    FailedPartitionSnapshot _partition;

    void Establish() => Configure(AlertsWith("partition-failing", new AlertConditionOptions { RaiseAfter = TimeSpan.FromMinutes(30) }));

    void Because()
    {
        _partition = FailedPartition(_id, TimeSpan.FromMinutes(10));
        _result = Evaluate(SnapshotOf(_partition));
    }

    [Fact] void should_not_raise_before_the_configured_time() => _result.Transitions.ShouldBeEmpty();
    [Fact] void should_report_the_configured_time_as_due() => _result.NextRaiseDue.ShouldEqual(_partition.FirstAttempt + TimeSpan.FromMinutes(30));
}
