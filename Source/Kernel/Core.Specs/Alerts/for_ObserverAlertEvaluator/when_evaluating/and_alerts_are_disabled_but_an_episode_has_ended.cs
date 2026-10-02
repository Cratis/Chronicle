// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Alerts.for_ObserverAlertEvaluator.when_evaluating;

public class and_alerts_are_disabled_but_an_episode_has_ended : given.an_evaluator
{
    ObserverAlertEvaluation _result;

    void Establish() => Configure(new() { Enabled = false });

    void Because() => _result = Evaluate(SnapshotOf(FailedPartition(FailedPartitionId.New(), TimeSpan.FromHours(1))), OpenQuarantineIncident());

    [Fact] void should_clear_the_ended_episode() => _result.Transitions.Single().ShouldBeOfExactType<AlertCleared>();
    [Fact] void should_not_raise_the_current_failure() => _result.Transitions.OfType<AlertRaised>().ShouldBeEmpty();
}
