// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer;

public class when_catch_up_concludes_without_moving_and_stale_retries_are_exhausted : given.an_observer_with_subscription
{
    int _timersBefore;

    async Task Establish()
    {
        await _observer.Subscribe<NullObserverSubscriber>(ObserverType.Reactor, [EventType.Unknown], SiloAddress.Zero);
        await _observer.CaughtUp(JobId.New(), 42L);
        _eventSequence.GetTailSequenceNumberForEventTypes(Arg.Any<IEnumerable<EventType>>()).Returns((EventSequenceNumber)100L);
        _observer.SetCatchupRecoveryAttemptsForTesting(3);
        _timersBefore = _silo.TimerRegistry.NumberOfActiveTimers;
        for (var lap = 0; lap < 3; lap++)
        {
            await _observer.CaughtUp(JobId.New(), 42L);
        }
    }

    async Task Because() => await _observer.CaughtUp(JobId.New(), 42L);

    [Fact] async Task should_back_off_on_the_fourth_stale_lap() => (await _observer.IsCatchUpBackedOff()).ShouldBeTrue();
    [Fact] void should_not_leave_a_short_retry_scheduled() => _silo.TimerRegistry.NumberOfActiveTimers.ShouldEqual(_timersBefore);
    [Fact] void should_preserve_the_recovery_attempts() => _observer.CatchupRecoveryAttempts.ShouldEqual(3);
}
