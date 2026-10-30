// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_spilling_to_catch_up_fails;

/// <summary>
/// A spill covers every subscriber of every queue, so a later successful spill settles the earlier failure and the
/// pending retry is no longer needed.
/// </summary>
public class and_a_later_append_spills_successfully : given.a_failed_spill
{
    async Task Because()
    {
        await AppendEvent();
        await AppendEvent();
    }

    [Fact] void should_spill_once_per_append() => _spillAttempts.ShouldEqual(2);
    [Fact] void should_release_the_retry_timer() => _silo.TimerRegistry.NumberOfActiveTimers.ShouldEqual(_timersBeforeAppend);
}
