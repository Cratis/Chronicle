// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Alerts;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Alerts.for_ObserverAlertEvaluator.when_evaluating;

/// <summary>
/// A quarantined observer raises a critical incident of its own, with a new identifier and no partition.
/// </summary>
public class and_observer_is_quarantined : given.an_evaluator
{
    readonly FailedPartitionId _failedPartitionId = FailedPartitionId.New();
    ObserverAlertEvaluation _result;
    AlertRaised _raised;
    FailedPartitionSnapshot _partition;

    void Because()
    {
        _partition = FailedPartition(_failedPartitionId, TimeSpan.FromMinutes(1), attemptCount: 7, failureKind: FailureKind.Handling, message: "Quarantine me");
        _result = Evaluate(SnapshotOf(_partition) with { IsQuarantined = true });
        _raised = _result.Transitions.OfType<AlertRaised>().Single(_ => _.Condition == AlertConditionKind.ObserverQuarantined);
    }

    [Fact] void should_use_a_new_incident_id() => _raised.IncidentId.ShouldNotEqual((IncidentId)_failedPartitionId);
    [Fact] void should_have_an_incident_id() => _raised.IncidentId.Value.ShouldNotEqual(Guid.Empty);
    [Fact] void should_be_critical() => _raised.Severity.ShouldEqual(AlertSeverity.Critical);
    [Fact] void should_target_the_observer_without_a_partition() => _raised.Target.ShouldEqual(AlertTarget.For(_observer, AlertPartition.None));
    [Fact] void should_carry_the_latest_failure_as_evidence() => _raised.Evidence.ShouldEqual(AlertEvidence.Create(7, _partition.FirstAttempt, _partition.LastAttempt, FailureKind.Handling, "Quarantine me"));
}
