// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Alerts;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Alerts.for_ObserverAlertEvaluator.when_evaluating;

/// <summary>
/// A quarantined partition with no open incident raises the retries exhausted condition straight away, even with the
/// maximum number of retries at 0.
/// </summary>
public class and_max_retry_attempts_is_infinite_and_partition_is_quarantined_without_an_open_incident : given.an_evaluator
{
    readonly FailedPartitionId _id = FailedPartitionId.New();
    ObserverAlertEvaluation _result;
    AlertRaised _raised;

    void Because()
    {
        _result = Evaluate(SnapshotOf(FailedPartition(_id, TimeSpan.FromMinutes(1), isQuarantined: true)) with { MaxRetryAttempts = 0 });
        _raised = _result.Transitions.OfType<AlertRaised>().Single();
    }

    [Fact] void should_only_raise() => _result.Transitions.Count.ShouldEqual(1);
    [Fact] void should_raise_the_retries_exhausted_condition() => _raised.Condition.ShouldEqual(AlertConditionKind.PartitionRetriesExhausted);
    [Fact] void should_be_critical() => _raised.Severity.ShouldEqual(AlertSeverity.Critical);
}
