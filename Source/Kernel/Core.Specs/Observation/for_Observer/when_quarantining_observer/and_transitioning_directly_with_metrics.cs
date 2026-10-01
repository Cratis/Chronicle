// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation.States;

namespace Cratis.Chronicle.Observation.for_Observer.when_quarantining_observer;

public class and_transitioning_directly_with_metrics : given.an_observer_with_metrics
{
    async Task Because()
    {
        await _observer.TransitionTo<QuarantinedObserver>();

        // Already quarantined, so this is not a transition and not another quarantine.
        await _observer.TransitionTo<QuarantinedObserver>();
    }

    [Fact] void should_count_the_observer_as_quarantined_once() => _metrics.SumOf(ObserverQuarantined).ShouldEqual(1);
    [Fact] void should_not_count_any_failed_partition() => _metrics.For(PartitionsFailed).ShouldBeEmpty();
}
