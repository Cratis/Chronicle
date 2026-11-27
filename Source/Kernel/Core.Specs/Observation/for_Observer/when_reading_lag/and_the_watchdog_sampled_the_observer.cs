// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_Observer.when_reading_lag;

public class and_the_watchdog_sampled_the_observer : given.an_observer_publishing_a_stalled_sample
{
    ObserverLagSample _afterRefresh;

    async Task Because()
    {
        await _observer.PartitionFailed("unrelated", 6UL, ["Failure"], "Stack");
        _afterRefresh = Published;
    }

    [Fact] void should_publish_the_sample_the_watchdog_took() => _afterRefresh.Status.ShouldEqual(ObserverLagStatus.Stalled);
    [Fact] void should_publish_its_distance() => _afterRefresh.Distance.ShouldEqual(17UL);
}
