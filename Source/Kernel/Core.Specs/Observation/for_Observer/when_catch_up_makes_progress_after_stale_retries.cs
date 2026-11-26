// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_Observer;

/// <summary>
/// Progress ends the retry sequence: the pending retry is dropped and the next stale lap gets a fresh allowance (#5351).
/// </summary>
public class when_catch_up_makes_progress_after_stale_retries : given.an_observer_with_subscription
{
    int _timersBefore;
    int _timersAfterProgress;

    async Task Establish()
    {
        await _observer.Subscribe<NullObserverSubscriber>(ObserverType.Reactor, [EventType.Unknown], SiloAddress.Zero);
        await _observer.CaughtUp(JobId.New(), 42L);
        await _silo.RunCatchUpContinuation();
        _timersBefore = _silo.TimerRegistry.NumberOfActiveTimers;
        _eventSequence
            .GetNextSequenceNumberGreaterOrEqualTo(Arg.Any<EventSequenceNumber>(), Arg.Any<IEnumerable<EventType>>())
            .Returns((EventSequenceNumber)50UL);
        await _observer.CaughtUp(JobId.New(), 42L);
        await _silo.RunCatchUpContinuation();
        for (var lap = 0; lap < 3; lap++)
        {
            await _observer.CaughtUp(JobId.New(), 42L);
            await _silo.RunCatchUpContinuation();
        }

        await _observer.CaughtUp(JobId.New(), 43L);
        await _silo.RunCatchUpContinuation();
        await _observer.CaughtUp(JobId.New(), 43L);
        await _silo.RunCatchUpContinuation();
        _timersAfterProgress = _silo.TimerRegistry.NumberOfActiveTimers;
    }

    async Task Because()
    {
        await _observer.CaughtUp(JobId.New(), 43L);
        await _silo.RunCatchUpContinuation();
    }

    [Fact] void should_drop_the_pending_retry_on_progress() => _timersAfterProgress.ShouldEqual(_timersBefore);
    [Fact] void should_restart_the_short_retry_allowance() => _silo.TimerRegistry.NumberOfActiveTimers.ShouldEqual(_timersAfterProgress + 1);
}
