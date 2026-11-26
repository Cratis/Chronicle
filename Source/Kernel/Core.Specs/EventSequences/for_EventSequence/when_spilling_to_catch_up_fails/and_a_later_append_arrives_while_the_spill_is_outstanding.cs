// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_spilling_to_catch_up_fails;

/// <summary>
/// While a spill is outstanding the queues are not reachable, so a later append must not try live delivery against
/// them and wait for another failure; it asks for a spill, which reads its event from storage like every other.
/// </summary>
public class and_a_later_append_arrives_while_the_spill_is_outstanding : given.a_failed_spill
{
    protected override int FailingSpillAttempts => 5;

    async Task Because()
    {
        await AppendEvent();
        await AppendEvent();
        await AppendEvent();
    }

    [Fact] void should_try_live_delivery_only_for_the_first_append() => _appendedEventsQueues.Received(1).Enqueue(Arg.Any<IEnumerable<AppendedEvent>>());
    [Fact] void should_ask_for_a_spill_for_every_append() => _spillAttempts.ShouldEqual(3);
    [Fact] void should_keep_a_single_retry_timer() => _silo.TimerRegistry.NumberOfActiveTimers.ShouldEqual(_timersBeforeAppend + 1);
}
