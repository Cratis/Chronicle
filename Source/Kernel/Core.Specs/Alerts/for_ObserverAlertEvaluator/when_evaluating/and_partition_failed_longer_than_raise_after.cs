// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Alerts;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Alerts.for_ObserverAlertEvaluator.when_evaluating;

/// <summary>
/// A partition that is still failing after the grace period raises a warning, with the identifier of the failed
/// partition as the incident identifier and the evidence of the failure.
/// </summary>
public class and_partition_failed_longer_than_raise_after : given.an_evaluator
{
    readonly FailedPartitionId _id = FailedPartitionId.New();
    ObserverAlertEvaluation _result;
    FailedPartitionSnapshot _partition;
    AlertRaised _raised;

    void Because()
    {
        _partition = FailedPartition(_id, TimeSpan.FromMinutes(6), attemptCount: 4, partition: "order-42", failureKind: FailureKind.Handling, message: "Boom");
        _result = Evaluate(SnapshotOf(_partition));
        _raised = _result.Transitions.OfType<AlertRaised>().Single();
    }

    [Fact] void should_only_raise() => _result.Transitions.Count.ShouldEqual(1);
    [Fact] void should_use_the_failed_partition_id_as_incident_id() => _raised.IncidentId.ShouldEqual((IncidentId)_id);
    [Fact] void should_raise_the_partition_failing_condition() => _raised.Condition.ShouldEqual(AlertConditionKind.PartitionFailing);
    [Fact] void should_raise_a_warning() => _raised.Severity.ShouldEqual(AlertSeverity.Warning);
    [Fact] void should_target_the_partition() => _raised.Target.ShouldEqual(AlertTarget.For(_observer, "order-42"));
    [Fact] void should_carry_the_attempt_count() => _raised.Evidence.AttemptCount.ShouldEqual(4);
    [Fact] void should_carry_when_it_first_failed() => _raised.Evidence.FirstFailure.ShouldEqual(_partition.FirstAttempt);
    [Fact] void should_carry_when_it_last_failed() => _raised.Evidence.LastFailure.ShouldEqual(_partition.LastAttempt);
    [Fact] void should_carry_the_failure_kind() => _raised.Evidence.FailureKind.ShouldEqual(FailureKind.Handling);
    [Fact] void should_carry_the_message() => _raised.Evidence.Message.ShouldEqual("Boom");
    [Fact] void should_not_wait_for_anything_else() => _result.NextRaiseDue.ShouldBeNull();
}
