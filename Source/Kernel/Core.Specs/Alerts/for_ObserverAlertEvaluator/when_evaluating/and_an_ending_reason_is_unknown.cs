// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Alerts;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Alerts.for_ObserverAlertEvaluator.when_evaluating;

public class and_an_ending_reason_is_unknown : given.an_evaluator
{
    ObserverAlertEvaluation _result;

    void Because() => _result = Evaluate(SnapshotOf(), OpenPartitionIncident(FailedPartitionId.New()), OpenQuarantineIncident());

    [Fact] void should_default_partition_endings_to_recovered() => _result.Transitions.OfType<AlertCleared>().Single(_ => _.Condition == AlertConditionKind.PartitionFailing).Reason.ShouldEqual(AlertClearedReason.Recovered);
    [Fact] void should_default_quarantine_endings_to_cleared() => _result.Transitions.OfType<AlertCleared>().Single(_ => _.Condition == AlertConditionKind.ObserverQuarantined).Reason.ShouldEqual(AlertClearedReason.Cleared);
}
