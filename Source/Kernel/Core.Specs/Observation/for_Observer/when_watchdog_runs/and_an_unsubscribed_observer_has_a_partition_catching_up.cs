// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Observation.for_Observer.when_watchdog_runs;

public class and_an_unsubscribed_observer_has_a_partition_catching_up : given.an_idle_observer
{
    async Task Establish() => await _observer.RegisterCatchingUpPartitions([(Key)"partition"]);

    async Task Because()
    {
        await RunWatchdogAfter(TimeSpan.Zero);
        await RunWatchdogAfter(_idlePeriod * 2);
    }

    [Fact] void should_still_have_the_partition_catching_up() => _stateStorage.State.CatchingUpPartitions.ShouldContain((Key)"partition");
    [Fact] void should_keep_the_activation_for_the_catch_up() => ReleaseRequests.ShouldEqual(0);
}
