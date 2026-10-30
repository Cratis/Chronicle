// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_spilling_to_catch_up_fails;

/// <summary>
/// The queues can stay unreachable for longer than the first retries. The retry must keep going rather than give up
/// and leave the observers to their watchdog.
/// </summary>
public class and_the_retry_keeps_failing : given.a_failed_spill
{
    protected override int FailingSpillAttempts => int.MaxValue;

    async Task Because()
    {
        await AppendEvent();
        for (var tick = 0; tick < 4; tick++)
        {
            await _silo.TimerRegistry.FireAllAsync();
        }
    }

    [Fact] void should_retry_the_spill_on_every_tick() => _spillAttempts.ShouldEqual(5);
    [Fact] void should_keep_one_retry_timer_scheduled() => _silo.TimerRegistry.NumberOfActiveTimers.ShouldEqual(_timersBeforeAppend + 1);
}
