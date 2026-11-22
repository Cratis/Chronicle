// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_Observer.when_activating_a_resident_observer;

public class and_it_is_subscribed : given.an_observer_with_subscription
{
    [Fact] void should_have_a_watchdog_timer_of_its_own() => _silo.TimerRegistry.NumberOfActiveTimers.ShouldBeGreaterThan(0);
}
