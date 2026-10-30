// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_spilling_to_catch_up_fails;

/// <summary>
/// A failed spill leaves durable events with no path to their observers. It must be retried promptly instead of
/// waiting for the observers' watchdog, and the retry must stop once the spill lands.
/// </summary>
public class and_the_retry_succeeds : given.a_failed_spill
{
    int _timersAfterFailure;

    async Task Because()
    {
        await AppendEvent();
        _timersAfterFailure = _silo.TimerRegistry.NumberOfActiveTimers;
        await _silo.TimerRegistry.FireAllAsync();
    }

    [Fact] void should_schedule_a_retry_when_the_spill_fails() => _timersAfterFailure.ShouldEqual(_timersBeforeAppend + 1);
    [Fact] void should_retry_the_spill() => _spillAttempts.ShouldEqual(2);
    [Fact] void should_release_the_retry_timer_once_the_spill_lands() => _silo.TimerRegistry.NumberOfActiveTimers.ShouldEqual(_timersBeforeAppend);
}
