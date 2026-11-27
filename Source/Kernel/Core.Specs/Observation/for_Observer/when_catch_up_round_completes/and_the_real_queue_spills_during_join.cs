// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.for_Observer.when_catch_up_round_completes;

public class and_the_real_queue_spills_during_join : and_p_is_permanently_failed_on_the_real_queue
{
    protected override bool SpillDuringJoin => true;

    [Fact] void should_observe_the_first_lane_close_before_cutover() => ClosedDuringJoin.ShouldBeTrue();
    [Fact] void should_deliver_the_spilled_suffix() => _delivered.Where(number => number >= 14UL && number <= 16UL)
        .Distinct().Order().ShouldEqual([(EventSequenceNumber)14UL, (EventSequenceNumber)15UL, (EventSequenceNumber)16UL]);
}
