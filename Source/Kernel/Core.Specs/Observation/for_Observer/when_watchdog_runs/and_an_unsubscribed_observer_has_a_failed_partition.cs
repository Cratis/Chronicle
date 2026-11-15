// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_Observer.when_watchdog_runs;

public class and_an_unsubscribed_observer_has_a_failed_partition : given.an_idle_observer
{
    async Task Establish() => await _observer.PartitionFailed("partition", 42UL, ["Failed"], "Stack");

    async Task Because()
    {
        await RunWatchdogAfter(TimeSpan.Zero);
        await RunWatchdogAfter(_idlePeriod * 2);
    }

    [Fact] void should_keep_the_activation_for_the_failed_partition() => ReleaseRequests.ShouldEqual(0);
}
