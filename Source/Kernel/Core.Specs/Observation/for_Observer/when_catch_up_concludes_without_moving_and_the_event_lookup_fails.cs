// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using LookupResult = Cratis.Monads.Result<Cratis.Chronicle.Concepts.Events.EventSequenceNumber, Cratis.Chronicle.EventSequences.GetSequenceNumberError>;

namespace Cratis.Chronicle.Observation.for_Observer;

/// <summary>
/// A failed event lookup (one that throws) is not an answer: the zero-work backoff applies and the failure is not propagated (#5351).
/// </summary>
public class when_catch_up_concludes_without_moving_and_the_event_lookup_fails : given.an_observer_with_subscription
{
    int _timersBefore;
    Exception _error;
    bool _failNextLookup;

    async Task Establish()
    {
        await _observer.Subscribe<NullObserverSubscriber>(ObserverType.Reactor, [EventType.Unknown], SiloAddress.Zero);
        await _observer.CaughtUp(JobId.New(), 42L);
        await _silo.RunCatchUpContinuation();
        _eventSequence
            .GetNextSequenceNumberGreaterOrEqualTo(Arg.Any<EventSequenceNumber>(), Arg.Any<IEnumerable<EventType>>())
            .Returns(_ => FailOnce());
        await _observer.CaughtUp(JobId.New(), 42L);
        await _silo.RunCatchUpContinuation();
        _timersBefore = _silo.TimerRegistry.NumberOfActiveTimers;
        _failNextLookup = true;
    }

    async Task Because()
    {
        await _observer.CaughtUp(JobId.New(), 42L);
        _error = await Catch.Exception(() => _silo.RunCatchUpContinuation());
    }

    Task<LookupResult> FailOnce()
    {
        if (!_failNextLookup) return Task.FromResult<LookupResult>(EventSequenceNumber.Unavailable);
        _failNextLookup = false;
        return Task.FromException<LookupResult>(new InvalidOperationException("Lookup failed"));
    }

    [Fact] void should_not_propagate_the_lookup_failure() => _error.ShouldBeNull();
    [Fact] async Task should_back_off() => (await _observer.IsCatchUpBackedOff()).ShouldBeTrue();
    [Fact] void should_not_schedule_a_short_retry() => _silo.TimerRegistry.NumberOfActiveTimers.ShouldEqual(_timersBefore);
}
