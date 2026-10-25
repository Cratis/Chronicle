// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.for_Observer.when_partition_replayed.having_read_nothing;

public class and_a_selected_type_job_held_back_from_first_delivers_the_event : given.an_observer_whose_partition_jobs_deliver_to_a_subscriber
{
    void Establish()
    {
        _heldBackFrom = EventSequenceNumber.First;
        _liveSequenceNumber = EventSequenceNumber.First;
    }

    async Task Because() => await ReplayThenDeliverHeldBackEvents();

    [Fact] void should_hold_live_delivery_during_replay() => _deliveredWhileHeld.ShouldBeFalse();
    [Fact] void should_have_owned_the_partition_before_completion() => _heldBeforeCompletion.ShouldBeTrue();
    [Fact] void should_read_no_selected_events() => _replayDeliveries.ShouldBeEmpty();
    [Fact] void should_not_invent_a_handled_watermark() => _replayHandled.ShouldEqual(EventSequenceNumber.Unavailable);
    [Fact] void should_deliver_the_held_back_event_to_the_subscriber() => CheckHeldBackDelivery();
    [Fact] void should_release_replay_ownership() => _stateStorage.State.ReplayingPartitions.ShouldNotContain(_partition);
}
