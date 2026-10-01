// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Alerts.for_ObserverAlertEvaluator.when_evaluating;

public class and_an_unknown_condition_is_open_for_an_exhausted_partition : given.an_evaluator
{
    readonly FailedPartitionId _id = FailedPartitionId.New();
    OpenIncident _incident;
    ObserverAlertEvaluation _result;

    void Establish() => _incident = OpenPartitionIncident(_id, "observer-stalled");

    void Because() => _result = Evaluate(SnapshotOf(FailedPartition(_id, TimeSpan.FromMinutes(30), isQuarantined: true, attemptCount: 11)), _incident);

    [Fact] void should_not_produce_any_transitions() => _result.Transitions.ShouldBeEmpty();
    [Fact] void should_leave_the_incident_unchanged() => _result.ApplyTo([_incident]).ShouldContainOnly(_incident);
}
