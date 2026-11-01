// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.Jobs.for_HandleEventsForPartition.when_routing_controlled_delivery;

public class and_a_replay_waits_for_a_handler : given.a_controlled_job_step_with_observer_filters
{
    void Establish()
    {
        _performState.EventObservationState = EventObservationState.Replay;
        _performState.EndEventSequenceNumber = 100UL;
        _eventCursor.Current.Returns([CreateTypedEvent(_e, 2), CreateTypedEvent(_a, 3)]);
    }

    async Task Because() => await PerformStep();

    [Fact] void should_hold_even_below_the_live_join_fence() => _observer.Received(1).PartitionHeld(
        (Key)"some-partition",
        new FailedPartitionRecoveryCursor(2UL, FailedPartitionRecoveryMode.Replay, 100UL),
        _e.Id,
        Arg.Any<FailedPartitionId?>(),
        Arg.Any<Guid?>());
    [Fact] void should_not_deliver_a_later_event() => _handledBatches.ShouldBeEmpty();
    [Fact] void should_not_report_replay_success() => _succeeded.ShouldBeFalse();
}
