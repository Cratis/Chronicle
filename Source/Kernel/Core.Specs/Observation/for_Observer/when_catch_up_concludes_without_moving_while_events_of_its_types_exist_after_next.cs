// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.States;

namespace Cratis.Chronicle.Observation.for_Observer;

/// <summary>
/// A catch-up lap that moves nothing while events of the observer's types exist from where it is retries shortly (#5351).
/// </summary>
public class when_catch_up_concludes_without_moving_while_events_of_its_types_exist_after_next : given.an_observer_with_subscription
{
    int _timersBefore;

    async Task Establish()
    {
        await _observer.Subscribe<NullObserverSubscriber>(ObserverType.Reactor, [EventType.Unknown], SiloAddress.Zero);
        await _observer.CaughtUp(JobId.New(), 42L);
        await _silo.RunCatchUpContinuation();
        _eventSequence
            .GetNextSequenceNumberGreaterOrEqualTo(Arg.Any<EventSequenceNumber>(), Arg.Any<IEnumerable<EventType>>())
            .Returns((EventSequenceNumber)50UL);
        await _observer.CaughtUp(JobId.New(), 42L);
        await _silo.RunCatchUpContinuation();
        _timersBefore = _silo.TimerRegistry.NumberOfActiveTimers;
    }

    async Task Because()
    {
        await _observer.CaughtUp(JobId.New(), 42L);
        await _silo.RunCatchUpContinuation();
    }

    [Fact] async Task should_settle_in_observing() => (await _observer.GetCurrentState()).ShouldBeOfExactType<Observing>();
    [Fact] void should_schedule_a_short_retry() => _silo.TimerRegistry.NumberOfActiveTimers.ShouldEqual(_timersBefore + 1);
}
