// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer;

public class when_catch_up_makes_progress_after_stale_retries : given.an_observer_with_subscription
{
    int _timersAfterProgress;
    int _recoveryAttemptsAfterProgress;

    async Task Establish()
    {
        await _observer.Subscribe<NullObserverSubscriber>(ObserverType.Reactor, [EventType.Unknown], SiloAddress.Zero);
        await _observer.CaughtUp(JobId.New(), 42L);
        _eventSequence.GetTailSequenceNumberForEventTypes(Arg.Any<IEnumerable<EventType>>()).Returns((EventSequenceNumber)100L);
        for (var lap = 0; lap < 3; lap++)
        {
            await _observer.CaughtUp(JobId.New(), 42L);
        }

        _observer.SetCatchupRecoveryAttemptsForTesting(3);
        await _observer.CaughtUp(JobId.New(), 43L);
        _timersAfterProgress = _silo.TimerRegistry.NumberOfActiveTimers;
        _recoveryAttemptsAfterProgress = _observer.CatchupRecoveryAttempts;
    }

    async Task Because() => await _observer.CaughtUp(JobId.New(), 43L);

    [Fact] void should_restart_the_short_retry_allowance() => _silo.TimerRegistry.NumberOfActiveTimers.ShouldEqual(_timersAfterProgress + 1);
    [Fact] async Task should_not_back_off() => (await _observer.IsCatchUpBackedOff()).ShouldBeFalse();
    [Fact] void should_reset_the_recovery_attempts_on_progress() => _recoveryAttemptsAfterProgress.ShouldEqual(0);
}
