// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_Observer;

/// <summary>
/// A catch-up lap that moves nothing while no events of the observer's types exist from where it is keeps the zero-work backoff (#5351).
/// </summary>
public class when_catch_up_concludes_without_moving_and_no_events_of_its_types_exist_after_next : given.an_observer_with_subscription
{
    int _timersBefore;

    async Task Establish()
    {
        await _observer.Subscribe<NullObserverSubscriber>(ObserverType.Reactor, [EventType.Unknown], SiloAddress.Zero);
        await _observer.CaughtUp(JobId.New(), 42L);
        await _observer.CaughtUp(JobId.New(), 42L);
        _timersBefore = _silo.TimerRegistry.NumberOfActiveTimers;
    }

    async Task Because() => await _observer.CaughtUp(JobId.New(), 42L);

    [Fact] async Task should_back_off() => (await _observer.IsCatchUpBackedOff()).ShouldBeTrue();
    [Fact] void should_not_schedule_a_short_retry() => _silo.TimerRegistry.NumberOfActiveTimers.ShouldEqual(_timersBefore);
}
