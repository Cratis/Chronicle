// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_Observer;

/// <summary>
/// Once the short retries are spent the zero-work backoff takes over (#5351).
/// </summary>
public class when_catch_up_concludes_without_moving_and_stale_retries_are_exhausted : given.an_observer_with_subscription
{
    int _timersBefore;

    async Task Establish()
    {
        await _observer.Subscribe<NullObserverSubscriber>(ObserverType.Reactor, [EventType.Unknown], SiloAddress.Zero);
        await _observer.CaughtUp(JobId.New(), 42L);
        _eventSequence
            .GetNextSequenceNumberGreaterOrEqualTo(Arg.Any<EventSequenceNumber>(), Arg.Any<IEnumerable<EventType>>())
            .Returns((EventSequenceNumber)50UL);
        await _observer.CaughtUp(JobId.New(), 42L);
        for (var lap = 0; lap < 3; lap++)
        {
            await _observer.CaughtUp(JobId.New(), 42L);
        }

        await _observer.CaughtUp(JobId.New(), 42L);
        _timersBefore = _silo.TimerRegistry.NumberOfActiveTimers;
    }

    async Task Because() => await _observer.CaughtUp(JobId.New(), 42L);

    [Fact] async Task should_back_off() => (await _observer.IsCatchUpBackedOff()).ShouldBeTrue();
    [Fact] void should_not_leave_a_short_retry_scheduled() => _silo.TimerRegistry.NumberOfActiveTimers.ShouldEqual(_timersBefore);
}
