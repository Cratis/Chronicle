// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_Observer.when_watchdog_runs;

public class and_incidents_remain_open_without_failures : for_Observer.given.an_observer
{
    void Establish()
    {
        _observerAlerts.HasOpenIncidents().Returns(true);
        _observerAlerts.ClearReceivedCalls();
    }

    async Task Because()
    {
        await _observer.RunWatchdogAsync();
        await _observer.RunWatchdogAsync();
    }

    [Fact] void should_leave_persistence_retries_to_the_tracker() => _observerAlerts.ReceivedCalls().ShouldBeEmpty();
}
