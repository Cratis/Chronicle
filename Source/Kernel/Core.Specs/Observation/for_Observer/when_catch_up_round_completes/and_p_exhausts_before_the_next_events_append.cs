// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.for_Observer.when_catch_up_round_completes;

public class and_p_exhausts_before_the_next_events_append : given.an_observer_catching_up_during_appends
{
    protected override bool AppendBeforePReads => false;

    Task Because() => RunCatchUpDuringAppends();

    [Fact] void should_allow_q_to_read_past_the_common_prefix() => FirstLapDeliveries.ShouldContain((EventSequenceNumber)13UL);
    [Fact] void should_not_confirm_qs_position_as_a_common_frontier() => (AcceptedNext <= 12UL).ShouldBeTrue();
    [Fact] void should_deliver_the_applicable_events_after_restart() => _delivered.Distinct().Order().ShouldEqual([(EventSequenceNumber)10UL, (EventSequenceNumber)11UL, (EventSequenceNumber)12UL, (EventSequenceNumber)13UL]);
}
