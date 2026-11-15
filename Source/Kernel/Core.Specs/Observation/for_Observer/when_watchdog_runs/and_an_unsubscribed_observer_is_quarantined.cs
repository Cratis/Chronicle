// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.States;

namespace Cratis.Chronicle.Observation.for_Observer.when_watchdog_runs;

public class and_an_unsubscribed_observer_is_quarantined : given.an_idle_observer
{
    async Task Establish() => await ActivateAgainFrom(stored => stored with { RunningState = ObserverRunningState.Quarantined });

    async Task Because()
    {
        await RunWatchdogAfter(TimeSpan.Zero);
        await RunWatchdogAfter(_idlePeriod * 2);
    }

    [Fact] async Task should_still_be_quarantined() => (await _observer.GetCurrentState()).ShouldBeOfExactType<QuarantinedObserver>();
    [Fact] void should_keep_the_activation_while_quarantined() => ReleaseRequests.ShouldEqual(0);
}
