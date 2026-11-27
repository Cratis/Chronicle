// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.for_Observer.when_catch_up_round_completes;

public class and_p_exhausts_before_the_next_events_append : given.an_observer_catching_up_during_appends
{
    protected override bool AppendBeforePReads => false;

    Task Because() => RunCatchUpDuringAppends();

    [Fact] void should_only_deliver_the_original_interval_in_the_first_lap() => FirstLapDeliveries.Order().ShouldEqual([(EventSequenceNumber)10UL, (EventSequenceNumber)11UL]);
    [Fact] void should_durably_accept_the_end_of_the_original_interval() => AcceptedNext.ShouldEqual((EventSequenceNumber)12UL);
    [Fact] void should_restart_at_the_first_position_outside_the_original_interval() => SuccessorStart.ShouldEqual((EventSequenceNumber?)12UL);
    [Fact] void should_send_each_event_once_across_the_two_laps() => _delivered.Order().ShouldEqual([(EventSequenceNumber)10UL, (EventSequenceNumber)11UL, (EventSequenceNumber)12UL, (EventSequenceNumber)13UL]);
}
