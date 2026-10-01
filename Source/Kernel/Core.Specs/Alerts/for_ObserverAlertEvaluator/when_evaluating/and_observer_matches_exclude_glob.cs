// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

using AlertsOptions = Cratis.Chronicle.Configuration.Alerts;

namespace Cratis.Chronicle.Alerts.for_ObserverAlertEvaluator.when_evaluating;

/// <summary>
/// An observer whose identifier matches an excluded glob raises nothing, for a failing partition, an exhausted one and
/// a quarantine alike.
/// </summary>
public class and_observer_matches_exclude_glob : given.an_evaluator
{
    ObserverAlertEvaluation _result;

    void Establish()
    {
        Configure(new AlertsOptions { ExcludeObservers = ["orders-*"] });
    }

    void Because() => _result = Evaluate(SnapshotOf(
        FailedPartition(FailedPartitionId.New(), TimeSpan.FromHours(1)),
        FailedPartition(FailedPartitionId.New(), TimeSpan.FromHours(1), isQuarantined: true, partition: "other")) with { IsQuarantined = true });

    [Fact] void should_not_transition() => _result.Transitions.ShouldBeEmpty();
    [Fact] void should_not_wait_for_anything() => _result.NextRaiseDue.ShouldBeNull();
}
