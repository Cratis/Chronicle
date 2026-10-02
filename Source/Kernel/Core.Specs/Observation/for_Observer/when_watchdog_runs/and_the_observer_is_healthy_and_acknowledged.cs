// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_Observer.when_watchdog_runs;

public class and_the_observer_is_healthy_and_acknowledged : for_Observer.given.an_observer
{
    async Task Because()
    {
        await _observer.RunWatchdogAsync();
        await _observer.RunWatchdogAsync();
    }

    [Fact] void should_not_call_the_tracker() => _observerAlerts.ReceivedCalls().ShouldBeEmpty();
}
