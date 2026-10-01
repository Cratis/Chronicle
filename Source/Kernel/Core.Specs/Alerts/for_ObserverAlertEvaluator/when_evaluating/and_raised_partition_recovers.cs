// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Alerts;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Alerts.for_ObserverAlertEvaluator.when_evaluating;

/// <summary>
/// When the partition of an open incident is no longer failing the incident clears as recovered, once, with the
/// partition it was about.
/// </summary>
public class and_raised_partition_recovers : given.an_evaluator
{
    readonly FailedPartitionId _id = FailedPartitionId.New();
    ObserverAlertEvaluation _result;
    AlertCleared _cleared;

    void Because()
    {
        _result = Evaluate(SnapshotOf() with { Endings = new Dictionary<IncidentId, AlertClearedReason> { [_id] = AlertClearedReason.Recovered } }, OpenPartitionIncident(_id));
        _cleared = _result.Transitions.OfType<AlertCleared>().Single();
    }

    [Fact] void should_only_clear() => _result.Transitions.Count.ShouldEqual(1);
    [Fact] void should_clear_the_incident() => _cleared.IncidentId.ShouldEqual((IncidentId)_id);
    [Fact] void should_clear_it_as_recovered() => _cleared.Reason.ShouldEqual(AlertClearedReason.Recovered);
    [Fact] void should_keep_the_condition_it_had() => _cleared.Condition.ShouldEqual(AlertConditionKind.PartitionFailing);
    [Fact] void should_target_the_partition() => _cleared.Target.ShouldEqual(AlertTarget.For(_observer, "partition"));
}
