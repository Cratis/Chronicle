// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.Jobs.for_HandleEventsForPartition.when_routing_controlled_delivery;

public class and_the_uncovered_event_is_excluded_by_filters : given.a_controlled_job_step_with_observer_filters
{
    void Establish() => _eventCursor.Current.Returns([CreateTypedEvent(_a, 10), CreateTypedEvent(_e, 11, false), CreateTypedEvent(_a, 12)]);

    async Task Because() => await PerformStep();

    [Fact] void should_deliver_the_eligible_events_in_order() => _handledBatches.SelectMany(batch => batch).ShouldEqual([new EventSequenceNumber(10), new EventSequenceNumber(12)]);
    [Fact] void should_not_park_for_an_excluded_event() => _observer.DidNotReceive().PartitionHeld(Arg.Any<Key>(), Arg.Any<FailedPartitionRecoveryCursor>(), Arg.Any<EventTypeId>(), Arg.Any<FailedPartitionId?>(), Arg.Any<Guid?>());
    [Fact] void should_complete_the_batch() => _succeeded.ShouldBeTrue();
}
