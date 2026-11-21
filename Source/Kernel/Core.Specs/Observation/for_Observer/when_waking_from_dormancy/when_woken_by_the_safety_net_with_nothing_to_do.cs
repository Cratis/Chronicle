// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_Observer.when_waking_from_dormancy;

public class when_woken_by_the_safety_net_with_nothing_to_do : given.a_dormant_client_observer
{
    async Task Because()
    {
        await Wake();
        _silo.GrainRuntime.Mock.Invocations.Clear();
        await _observer.RunWatchdogOnce();
    }

    [Fact] void should_release_it_again_without_waiting_for_the_dormant_period() => ReleaseRequests.ShouldEqual(1);
    [Fact] void should_deliver_nothing() => _delivered.Count.ShouldEqual(0);
}
