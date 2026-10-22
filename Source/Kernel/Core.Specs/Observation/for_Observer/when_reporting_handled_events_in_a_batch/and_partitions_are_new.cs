// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_reporting_handled_events_in_a_batch;

public class and_partitions_are_new : given.an_observer
{
    async Task Because()
    {
        var step = JobStepId.New();
        await _observer.ReportHandledEventsBatch(step,
        [
            new("first", new Dictionary<EventTypeId, EventCount> { ["event"] = 2UL }, step, 3UL),
            new("second", new Dictionary<EventTypeId, EventCount> { ["event"] = 1UL }, step, 2UL)
        ]);
    }

    [Fact] void should_aggregate_the_totals() => _stateStorage.State.HandledEventCount.ShouldEqual((EventCount)3UL);
    [Fact] void should_aggregate_per_type() => _stateStorage.State.HandledEventCountPerEventType[(EventTypeId)"event"].ShouldEqual((EventCount)3UL);
    [Fact] void should_write_observer_state_once() => _storageStats.Writes.ShouldEqual(1);
    [Fact] void should_store_each_partition_count() => _observerHandledCountsStorage.Received(2).Increment(_observerId, Arg.Any<Key>(), Arg.Any<IReadOnlyDictionary<EventTypeId, EventCount>>());
}
