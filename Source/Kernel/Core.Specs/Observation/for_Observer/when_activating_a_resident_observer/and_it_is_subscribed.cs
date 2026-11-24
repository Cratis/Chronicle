// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_Observer.when_activating_a_resident_observer;

public class and_it_is_subscribed : given.an_observer_with_subscription
{
    [Fact] void should_register_with_the_supervisor_of_the_silo() => _supervisor.Received(1).Register(
        _observerKey.ToString(),
        TimeSpan.FromSeconds(_observersConfig.WatchdogInterval),
        Arg.Any<Func<Task<TimeSpan?>>>(),
        Arg.Any<Func<Task>>());

    async Task Because() => await _silo.TimerRegistry.FireAllAsync();

    [Fact] void should_not_leave_a_watchdog_timer_of_its_own() => _silo.TimerRegistry.NumberOfActiveTimers.ShouldEqual(0);
}
