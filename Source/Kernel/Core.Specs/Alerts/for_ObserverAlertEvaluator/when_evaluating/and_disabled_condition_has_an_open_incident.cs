// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Alerts;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Configuration;

namespace Cratis.Chronicle.Alerts.for_ObserverAlertEvaluator.when_evaluating;

/// <summary>
/// Disabling a condition stops new raises but does not announce that a problem is gone: an incident that is already
/// open still clears when its partition recovers.
/// </summary>
public class and_disabled_condition_has_an_open_incident : given.an_evaluator
{
    readonly FailedPartitionId _id = FailedPartitionId.New();
    ObserverAlertEvaluation _result;

    void Establish() => Configure(AlertsWith("partition-failing", new AlertConditionOptions { Enabled = false }));

    void Because() => _result = Evaluate(SnapshotOf(), OpenPartitionIncident(_id));

    [Fact] void should_clear_the_incident() => _result.Transitions.OfType<AlertCleared>().Single().IncidentId.ShouldEqual((IncidentId)_id);
}
