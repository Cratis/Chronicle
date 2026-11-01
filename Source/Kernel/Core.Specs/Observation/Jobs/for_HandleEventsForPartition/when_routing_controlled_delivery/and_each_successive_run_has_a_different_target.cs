// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.Jobs.for_HandleEventsForPartition.when_routing_controlled_delivery;

public class and_each_successive_run_has_a_different_target : given.a_controlled_job_step_with_observer_filters
{
    async Task Establish()
    {
        var subscription = await _observer.GetSubscription();
        _observer.GetSubscription().Returns(subscription with
        {
            Targets =
            [
                new(SiloAddress.Zero) { DeclaredEventTypes = [_a] },
                new(SiloAddress.Zero) { DeclaredEventTypes = [_e] }
            ]
        });
        _eventCursor.Current.Returns([CreateTypedEvent(_a, 10), CreateTypedEvent(_e, 11), CreateTypedEvent(_a, 12)]);
    }

    async Task Because() => await PerformStep();

    [Fact] void should_deliver_all_events_in_order() => _handledBatches.SelectMany(batch => batch).ShouldEqual([new EventSequenceNumber(10), new(11), new(12)]);
    [Fact] void should_make_three_single_target_runs() => _handledBatches.Select(batch => batch.Length).ShouldEqual([1, 1, 1]);
    [Fact] void should_not_hold() => _observer.DidNotReceive().PartitionHeld(Arg.Any<Key>(), Arg.Any<FailedPartitionRecoveryCursor>(), Arg.Any<EventTypeId>(), Arg.Any<FailedPartitionId?>(), Arg.Any<Guid?>());
    [Fact] void should_complete() => _succeeded.ShouldBeTrue();
}
