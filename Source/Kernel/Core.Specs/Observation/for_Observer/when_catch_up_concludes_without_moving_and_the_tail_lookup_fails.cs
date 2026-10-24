// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer;

public class when_catch_up_concludes_without_moving_and_the_tail_lookup_fails : given.an_observer_with_subscription
{
    int _timersBefore;
    Exception _error;

    async Task Establish()
    {
        await _observer.Subscribe<NullObserverSubscriber>(ObserverType.Reactor, [EventType.Unknown], SiloAddress.Zero);
        await _observer.CaughtUp(JobId.New(), 42L);
        _eventSequence.GetTailSequenceNumberForEventTypes(Arg.Any<IEnumerable<EventType>>())
            .Returns(Task.FromException<EventSequenceNumber>(new InvalidOperationException("Tail lookup failed")));
        _timersBefore = _silo.TimerRegistry.NumberOfActiveTimers;
        await _observer.CaughtUp(JobId.New(), 42L);
    }

    async Task Because() => _error = await Catch.Exception(() => _observer.CaughtUp(JobId.New(), 42L));

    [Fact] void should_not_propagate_the_lookup_failure() => _error.ShouldBeNull();
    [Fact] async Task should_back_off_as_before() => (await _observer.IsCatchUpBackedOff()).ShouldBeTrue();
    [Fact] void should_not_schedule_a_short_retry() => _silo.TimerRegistry.NumberOfActiveTimers.ShouldEqual(_timersBefore);
}
