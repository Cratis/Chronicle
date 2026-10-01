// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation.States;

namespace Cratis.Chronicle.Observation.for_Observer;

public class when_reactivating_a_quarantined_observer_with_metrics : given.an_observer_with_metrics
{
    async Task Establish() => await _observer.TransitionTo<QuarantinedObserver>();

    async Task Because() => await Reactivate();

    [Fact] void should_not_count_the_resumed_quarantine_as_another_quarantine() => _metrics.SumOf(ObserverQuarantined).ShouldEqual(1);
}
