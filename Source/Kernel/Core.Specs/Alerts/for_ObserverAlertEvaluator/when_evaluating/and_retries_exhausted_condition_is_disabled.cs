// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Configuration;

namespace Cratis.Chronicle.Alerts.for_ObserverAlertEvaluator.when_evaluating;

/// <summary>
/// A disabled retries exhausted condition does not escalate the incident that is open, which stays a warning.
/// </summary>
public class and_retries_exhausted_condition_is_disabled : given.an_evaluator
{
    readonly FailedPartitionId _id = FailedPartitionId.New();
    ObserverAlertEvaluation _result;

    void Establish() => Configure(AlertsWith("partition-retries-exhausted", new AlertConditionOptions { Enabled = false }));

    void Because() => _result = Evaluate(SnapshotOf(FailedPartition(_id, TimeSpan.FromHours(1), isQuarantined: true)), OpenPartitionIncident(_id));

    [Fact] void should_not_transition() => _result.Transitions.ShouldBeEmpty();
}
