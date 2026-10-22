// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_reporting_handled_events_in_a_batch;

public class and_a_partition_repeats_an_earlier_report : given.an_observer
{
    async Task Because()
    {
        var step = JobStepId.New();
        var earlier = new HandledEventsReport("first", new Dictionary<EventTypeId, EventCount> { ["event"] = 2UL }, step, 3UL);
        await _observer.ReportHandledEvents(earlier.Partition, earlier.Counts, step, earlier.HandledUpTo);
        var next = new HandledEventsReport("second", new Dictionary<EventTypeId, EventCount> { ["event"] = 1UL }, step, 2UL);
        await _observer.ReportHandledEventsBatch(step, [earlier, earlier, next, next]);
    }

    [Fact] void should_take_each_partition_report_only_once() => _stateStorage.State.HandledEventCount.ShouldEqual((EventCount)3UL);
    [Fact] void should_keep_deduplication_per_partition() => _observerHandledCountsStorage.Received(2).Increment(_observerId, Arg.Any<Key>(), Arg.Any<IReadOnlyDictionary<EventTypeId, EventCount>>());
}
