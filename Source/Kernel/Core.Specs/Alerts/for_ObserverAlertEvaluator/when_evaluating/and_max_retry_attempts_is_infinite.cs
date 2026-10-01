// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Alerts.for_ObserverAlertEvaluator.when_evaluating;

/// <summary>
/// With the maximum number of retries at 0 a partition that is not quarantined is retried forever, so it is never out
/// of retries by itself and the incident is not escalated.
/// </summary>
public class and_max_retry_attempts_is_infinite : given.an_evaluator
{
    readonly FailedPartitionId _id = FailedPartitionId.New();
    ObserverAlertEvaluation _result;

    void Because() => _result = Evaluate(
        SnapshotOf(FailedPartition(_id, TimeSpan.FromHours(1), isQuarantined: false)) with { MaxRetryAttempts = 0 },
        OpenPartitionIncident(_id));

    [Fact] void should_not_transition() => _result.Transitions.ShouldBeEmpty();
}
