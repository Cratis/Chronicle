// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_Observer.when_watchdog_runs;

public class and_idle_release_is_disabled : given.an_idle_observer
{
    protected override int IdleDeactivationAfter => 0;

    async Task Because()
    {
        await RunWatchdogAfter(TimeSpan.Zero);
        await RunWatchdogAfter(_idlePeriod * 2);
    }

    [Fact] void should_keep_the_activation() => ReleaseRequests.ShouldEqual(0);
}
