// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.EventSequences;
using LookupResult = Cratis.Monads.Result<Cratis.Chronicle.Concepts.Events.EventSequenceNumber, Cratis.Chronicle.EventSequences.GetSequenceNumberError>;

namespace Cratis.Chronicle.Observation.for_Observer;

/// <summary>
/// A lookup that comes back as an error is not an answer: the zero-work backoff applies and no short retry is scheduled (#5351).
/// </summary>
public class when_catch_up_concludes_without_moving_and_the_event_lookup_returns_an_error : given.an_observer_with_subscription
{
    int _timersBefore;

    async Task Establish()
    {
        await _observer.Subscribe<NullObserverSubscriber>(ObserverType.Reactor, [EventType.Unknown], SiloAddress.Zero);
        await _observer.CaughtUp(JobId.New(), 42L);
        _eventSequence
            .GetNextSequenceNumberGreaterOrEqualTo(Arg.Any<EventSequenceNumber>(), Arg.Any<IEnumerable<EventType>>())
            .Returns(Task.FromResult<LookupResult>(GetSequenceNumberError.StorageError));
        await _observer.CaughtUp(JobId.New(), 42L);
        _timersBefore = _silo.TimerRegistry.NumberOfActiveTimers;
    }

    async Task Because() => await _observer.CaughtUp(JobId.New(), 42L);

    [Fact] async Task should_back_off() => (await _observer.IsCatchUpBackedOff()).ShouldBeTrue();
    [Fact] void should_not_schedule_a_short_retry() => _silo.TimerRegistry.NumberOfActiveTimers.ShouldEqual(_timersBefore);
}
