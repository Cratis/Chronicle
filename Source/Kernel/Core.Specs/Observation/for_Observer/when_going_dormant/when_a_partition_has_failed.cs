// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_Observer.when_going_dormant;

public class when_a_partition_has_failed : given.a_subscribed_client_observer
{
    async Task Establish() => await _observer.PartitionFailed("partition", 42UL, ["Failed"], "Stack");

    async Task Because()
    {
        await RunWatchdogAfter(TimeSpan.Zero);
        await RunWatchdogAfter(_dormantPeriod * 2);
    }

    [Fact] void should_keep_the_activation() => ReleaseRequests.ShouldEqual(0);
}
