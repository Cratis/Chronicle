// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.for_Observer.when_partition_replayed.having_read_nothing;

public class and_a_selected_type_job_held_back_live_delivery : given.an_observer_completing_an_empty_partition_replay
{
    async Task Because() => await CompleteEmptyReplay();

    [Fact] void should_hand_the_held_back_event_to_catchup_from_the_delivery_boundary() => CheckHeldBackEventIsHandedToCatchupFrom(45UL);
    [Fact] void should_not_deliver_the_event_during_replay() => CheckReplayDidNotDeliverTheHeldBackEvent();
    [Fact] void should_stop_holding_back_for_replay() => _stateStorage.State.ReplayingPartitions.ShouldNotContain(_partition);
    [Fact] void should_not_count_the_boundary_as_handled() => _stateStorage.State.LastHandledEventSequenceNumber.ShouldEqual((EventSequenceNumber)42UL);
}
