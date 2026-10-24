// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer;

public class when_catch_up_concludes_without_moving_while_no_events_of_its_types_exist_after_next : given.an_observer_with_subscription
{
    int _timersBefore;
    bool _backedOffAfterFirstLap;

    async Task Establish()
    {
        await _observer.Subscribe<NullObserverSubscriber>(ObserverType.Reactor, [EventType.Unknown], SiloAddress.Zero);
        await _observer.CaughtUp(JobId.New(), 42L);
        _eventSequence.GetTailSequenceNumberForEventTypes(Arg.Any<IEnumerable<EventType>>()).Returns((EventSequenceNumber)42L);
        _timersBefore = _silo.TimerRegistry.NumberOfActiveTimers;
        await _observer.CaughtUp(JobId.New(), 42L);
        _backedOffAfterFirstLap = await _observer.IsCatchUpBackedOff();
    }

    async Task Because() => await _observer.CaughtUp(JobId.New(), 42L);

    [Fact] void should_not_back_off_after_the_first_lap() => _backedOffAfterFirstLap.ShouldBeFalse();
    [Fact] async Task should_back_off_after_the_second_lap() => (await _observer.IsCatchUpBackedOff()).ShouldBeTrue();
    [Fact] void should_not_schedule_a_short_retry() => _silo.TimerRegistry.NumberOfActiveTimers.ShouldEqual(_timersBefore);
}
