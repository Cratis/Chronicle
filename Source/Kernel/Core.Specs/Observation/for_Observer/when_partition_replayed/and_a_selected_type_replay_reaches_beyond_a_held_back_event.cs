// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.for_Observer.when_partition_replayed;

public class and_a_selected_type_replay_reaches_beyond_a_held_back_event : given.an_observer_whose_partition_jobs_deliver_to_a_subscriber
{
    void Establish() => StoreSelectedEvent(50UL);

    async Task Because() => await ReplayThenDeliverHeldBackEvents();

    [Fact] void should_hold_live_delivery_during_replay() => _deliveredWhileHeld.ShouldBeFalse();
    [Fact] void should_have_owned_the_partition_before_completion() => _heldBeforeCompletion.ShouldBeTrue();
    [Fact] void should_deliver_the_later_selected_event_during_replay() =>
        _replayDeliveries.Count(_ => _.Partition == _partition && _.Event.Context.EventType == selected_type && _.Event.Context.SequenceNumber == (EventSequenceNumber)50UL).ShouldEqual(1);
    [Fact] void should_record_the_real_selected_type_handled_watermark() => _replayHandled.ShouldEqual((EventSequenceNumber)50UL);
    [Fact] void should_record_the_real_selected_type_scanned_watermark() => _replayScanned.ShouldEqual((EventSequenceNumber)50UL);
    [Fact] void should_still_deliver_the_earlier_unselected_event_to_the_subscriber() => CheckHeldBackDelivery();
    [Fact] void should_release_replay_ownership() => _stateStorage.State.ReplayingPartitions.ShouldNotContain(_partition);
}
