// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Alerts;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Alerts.for_ObserverAlertEvaluator.when_evaluating;

/// <summary>
/// A partition that recovered and then failed again is a new failed partition with a new identifier. The incident of
/// the earlier episode clears and the new episode is its own incident.
/// </summary>
public class and_partition_failure_is_a_new_episode : given.an_evaluator
{
    readonly FailedPartitionId _oldId = FailedPartitionId.New();
    readonly FailedPartitionId _newId = FailedPartitionId.New();
    ObserverAlertEvaluation _result;

    void Because() => _result = Evaluate(SnapshotOf(FailedPartition(_newId, TimeSpan.FromMinutes(10))), OpenPartitionIncident(_oldId));

    [Fact] void should_clear_the_old_incident() => _result.Transitions.OfType<AlertCleared>().Single().IncidentId.ShouldEqual((IncidentId)_oldId);
    [Fact] void should_raise_the_new_incident() => _result.Transitions.OfType<AlertRaised>().Single().IncidentId.ShouldEqual((IncidentId)_newId);
}
