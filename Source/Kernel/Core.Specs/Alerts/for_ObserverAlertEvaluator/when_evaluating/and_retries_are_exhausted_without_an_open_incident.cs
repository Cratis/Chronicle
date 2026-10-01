// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Alerts;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Alerts.for_ObserverAlertEvaluator.when_evaluating;

/// <summary>
/// A partition that has run out of retries and has no open incident - the grace period was never waited out, or the
/// kernel restarted - raises the retries exhausted condition straight away. A person has to act, so there is nothing
/// to wait for.
/// </summary>
public class and_retries_are_exhausted_without_an_open_incident : given.an_evaluator
{
    readonly FailedPartitionId _id = FailedPartitionId.New();
    ObserverAlertEvaluation _result;
    AlertRaised _raised;

    void Because()
    {
        _result = Evaluate(SnapshotOf(FailedPartition(_id, TimeSpan.FromMinutes(1), isQuarantined: true)));
        _raised = _result.Transitions.OfType<AlertRaised>().Single();
    }

    [Fact] void should_only_raise() => _result.Transitions.Count.ShouldEqual(1);
    [Fact] void should_use_the_failed_partition_id_as_incident_id() => _raised.IncidentId.ShouldEqual((IncidentId)_id);
    [Fact] void should_raise_the_retries_exhausted_condition() => _raised.Condition.ShouldEqual(AlertConditionKind.PartitionRetriesExhausted);
    [Fact] void should_be_critical() => _raised.Severity.ShouldEqual(AlertSeverity.Critical);
}
