// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Alerts;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Alerts.for_ObserverAlertEvaluator.when_evaluating;

public class and_failed_partitions_are_cleared : given.an_evaluator
{
    readonly FailedPartitionId _failingId = FailedPartitionId.New();
    readonly FailedPartitionId _exhaustedId = FailedPartitionId.New();
    ObserverAlertEvaluation _result;

    void Because() => _result = Evaluate(
        SnapshotOf() with { Endings = new Dictionary<Guid, AlertClearedReason> { [_failingId.Value] = AlertClearedReason.Cleared, [_exhaustedId.Value] = AlertClearedReason.Cleared } },
        OpenPartitionIncident(_failingId),
        OpenPartitionIncident(_exhaustedId, AlertConditionKind.PartitionRetriesExhausted, AlertSeverity.Critical, "other"));

    [Fact] void should_clear_both_incidents() => _result.Transitions.OfType<AlertCleared>().Select(_ => _.IncidentId).ShouldContainOnly((IncidentId)_failingId, (IncidentId)_exhaustedId);
    [Fact] void should_report_the_operator_clear() => _result.Transitions.OfType<AlertCleared>().All(_ => _.Reason == AlertClearedReason.Cleared).ShouldBeTrue();
    [Fact] void should_keep_each_partition() => _result.Transitions.OfType<AlertCleared>().Select(_ => _.Target.Partition).ShouldContainOnly((AlertPartition)"partition", (AlertPartition)"other");
}
