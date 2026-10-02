// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Alerts.for_ObserverAlertEvaluator.when_evaluating;

public class and_the_latest_snapshot_is_empty : given.an_evaluator
{
    ObserverAlertEvaluation _result;

    void Because() => _result = Evaluate(SnapshotOf(), OpenPartitionIncident(FailedPartitionId.New()), OpenQuarantineIncident());

    [Fact] void should_clear_every_ended_episode() => _result.Transitions.OfType<AlertCleared>().Count().ShouldEqual(2);
    [Fact] void should_not_raise_anything() => _result.Transitions.OfType<AlertRaised>().ShouldBeEmpty();
    [Fact] void should_not_retain_a_grace_deadline() => _result.NextRaiseDue.ShouldBeNull();
}
