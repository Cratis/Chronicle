// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Alerts.for_ObserverAlertEvaluator.when_evaluating;

/// <summary>
/// Only the kernel's alert observers are left out. Other kernel owned observers are alerted about like any other.
/// </summary>
public class and_observer_is_another_kernel_observer : given.an_evaluator
{
    ObserverAlertEvaluation _result;

    void Because() => _result = Evaluate(
        SnapshotOf(FailedPartition(FailedPartitionId.New(), TimeSpan.FromHours(1))) with
        {
            Observer = _observer with { ObserverId = "$system.Cratis.Chronicle.Observation.EventStoreSubscriptions.EventStoreSubscriptionsReactor" }
        });

    [Fact] void should_raise() => _result.Transitions.OfType<AlertRaised>().Count().ShouldEqual(1);
}
