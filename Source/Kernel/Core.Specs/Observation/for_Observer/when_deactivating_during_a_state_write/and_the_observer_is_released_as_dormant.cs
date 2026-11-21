// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_Observer.when_deactivating_during_a_state_write;

public class and_the_observer_is_released_as_dormant : for_Observer.when_going_dormant.given.a_subscribed_client_observer
{
    Exception? _releaseError;
    Exception? _lateTurnError;

    async Task Establish()
    {
        await RunWatchdogAfter(TimeSpan.Zero);
        await RunWatchdogAfter(_dormantPeriod);
        _appendedEventsQueues.ClearReceivedCalls();
    }

    async Task Because()
    {
        _releaseError = await Catch.Exception(Deactivate);

        // A callback the runtime does not wait for (a timer or a reminder that was already running) reaches the observer
        // after the release has finished and the grain was disposed.
        _lateTurnError = await Catch.Exception(async () => await _observer.ReportAlertState());
    }

    [Fact] async Task should_have_kept_the_subscription_for_the_release() => await _appendedEventsQueues.Received(1).TryMarkDormant(_observerKey);
    [Fact] void should_complete_the_release() => _releaseError.ShouldBeNull();
    [Fact] void should_let_a_late_turn_finish_without_failing() => _lateTurnError.ShouldBeNull();
}
