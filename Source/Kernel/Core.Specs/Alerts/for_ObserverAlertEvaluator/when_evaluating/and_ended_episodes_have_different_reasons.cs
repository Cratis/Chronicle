// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Alerts;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Alerts.for_ObserverAlertEvaluator.when_evaluating;

public class and_ended_episodes_have_different_reasons : given.an_evaluator
{
    readonly FailedPartitionId _cleared = FailedPartitionId.New();
    readonly FailedPartitionId _recovered = FailedPartitionId.New();
    ObserverAlertEvaluation _result;

    void Because() => _result = Evaluate(SnapshotOf() with
    {
        Endings = new Dictionary<IncidentId, AlertClearedReason> { [_cleared] = AlertClearedReason.Cleared, [_recovered] = AlertClearedReason.Recovered }
    }, OpenPartitionIncident(_cleared), OpenPartitionIncident(_recovered));

    [Fact] void should_preserve_the_operator_clear() => _result.Transitions.OfType<AlertCleared>().Single(_ => _.IncidentId == (IncidentId)_cleared).Reason.ShouldEqual(AlertClearedReason.Cleared);
    [Fact] void should_preserve_the_recovery() => _result.Transitions.OfType<AlertCleared>().Single(_ => _.IncidentId == (IncidentId)_recovered).Reason.ShouldEqual(AlertClearedReason.Recovered);
}
