// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;
using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.for_Observer.when_catch_up_round_completes;

public class and_live_delivery_settles_after_another_applicable_prefix_event : given.an_observer_splitting_a_catch_up_prefix
{
    protected override bool IncludeApplicableEventAtFourteen => true;

    async Task Because()
    {
        await SetUpPrefix();
        await _observer.Handle("Q", [_liveEvent]);
        await _observer.CaughtUp(JobId.NotSet, 13UL, 13UL);
        await Join();
    }

    [Fact] void should_keep_the_unproved_prefix_restartable() => (_accepted <= 11UL).ShouldBeTrue();
    [Fact] void should_deliver_p() => _delivered.ShouldContain((EventSequenceNumber)11UL);
    [Fact] void should_deliver_t_if_applicable() => _delivered.Contains((EventSequenceNumber)15UL).ShouldEqual(HoldT);
    [Fact] void should_deliver_the_applicable_event_at_fourteen() => _delivered.ShouldContain((EventSequenceNumber)14UL);
    [Fact] void should_deliver_the_admitted_event() => _delivered.ShouldContain((EventSequenceNumber)16UL);
    [Fact] void should_deliver_the_later_event() => _delivered.ShouldContain((EventSequenceNumber)17UL);
}
