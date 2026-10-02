// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Alerts;
using Cratis.Chronicle.Concepts.Observation;

using AlertsOptions = Cratis.Chronicle.Configuration.Alerts;

namespace Cratis.Chronicle.Alerts.for_ObserverAlertEvaluator.when_evaluating;

/// <summary>
/// Excluding an observer stops new alerts, but an incident that is already open still clears.
/// </summary>
public class and_excluded_observer_has_an_open_incident : given.an_evaluator
{
    readonly FailedPartitionId _id = FailedPartitionId.New();
    ObserverAlertEvaluation _result;

    void Establish() => Configure(new AlertsOptions { ExcludeObservers = ["orders-projection"] });

    void Because() => _result = Evaluate(SnapshotOf(), OpenPartitionIncident(_id));

    [Fact] void should_clear_the_incident() => _result.Transitions.OfType<AlertCleared>().Single().IncidentId.ShouldEqual((IncidentId)_id);
}
