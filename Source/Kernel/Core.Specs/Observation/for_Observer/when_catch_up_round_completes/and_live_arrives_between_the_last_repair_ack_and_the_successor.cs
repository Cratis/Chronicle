// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;
using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.for_Observer.when_catch_up_round_completes;

public class and_live_arrives_between_the_last_repair_ack_and_the_successor : given.an_observer_splitting_a_catch_up_prefix
{
    protected virtual bool DiscardActivation => false;

    async Task Because()
    {
        await SetUpPrefix();
        await _observer.CaughtUp(JobId.NotSet, 13UL, 13UL);
        await _observer.Handle("Q", [_liveEvent]);
        if (DiscardActivation) await Crash();
        await Join();
    }

    [Fact] void should_leave_the_held_events_restartable() => (_accepted <= 11UL).ShouldBeTrue();
    [Fact] void should_deliver_the_held_t() => _delivered.ShouldContain((EventSequenceNumber)15UL);
    [Fact] void should_deliver_the_later_q() => _delivered.ShouldContain((EventSequenceNumber)16UL);
}
