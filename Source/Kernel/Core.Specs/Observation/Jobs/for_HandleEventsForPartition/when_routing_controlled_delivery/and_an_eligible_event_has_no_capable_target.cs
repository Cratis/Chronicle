// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.Jobs.for_HandleEventsForPartition.when_routing_controlled_delivery;

public class and_an_eligible_event_has_no_capable_target : given.a_controlled_job_step_with_observer_filters
{
    void Establish()
    {
        _performState.EventObservationState = EventObservationState.None;
        _eventCursor.Current.Returns([CreateTypedEvent(_a, 10), CreateTypedEvent(_e, 11), CreateTypedEvent(_a, 12)]);
    }

    async Task Because() => await PerformStep();

    [Fact] void should_only_deliver_the_prefix_before_the_uncovered_event() => _handledBatches.SelectMany(batch => batch).ShouldEqual([new EventSequenceNumber(10)]);
    [Fact] void should_park_at_the_exact_first_uncovered_event() => _observer.Received(1).PartitionHeld(
        (Key)"some-partition",
        Arg.Is<FailedPartitionRecoveryCursor>(cursor => cursor.Position == (EventSequenceNumber)11 && cursor.Mode == FailedPartitionRecoveryMode.Live),
        _e.Id,
        Arg.Any<FailedPartitionId?>(),
        Arg.Any<Guid?>());
    [Fact] void should_not_report_full_success() => _succeeded.ShouldBeFalse();
}
