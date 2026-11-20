// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_Observer.when_waking_from_dormancy;

public class when_woken_by_an_event : given.a_dormant_client_observer
{
    void Establish() => Append(EventAt(0));

    async Task Because()
    {
        await Wake();
        _silo.GrainRuntime.Mock.Invocations.Clear();
        await _observer.RunWatchdogOnce();
    }

    [Fact] void should_deliver_the_event() => _delivered.Count.ShouldEqual(1);
    [Fact] void should_stay_for_the_dormant_period_as_it_has_just_worked() => ReleaseRequests.ShouldEqual(0);
}
