// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_reporting_handled_events_in_a_batch;

public class and_one_batch_fills_the_pending_partition_buffer : given.an_observer_coalescing_writes
{
    int _writesBeforeFlush;

    async Task Because()
    {
        var step = JobStepId.New();
        var reports = Enumerable.Range(0, 600).Select(i => new HandledEventsReport($"partition-{i}", new Dictionary<EventTypeId, EventCount> { ["event"] = 1UL }, step, (ulong)i)).ToArray();
        await _observer.ReportHandledEventsBatch(step, reports);
        _writesBeforeFlush = _storageStats.Writes;
        await _observer.RunWatchdogAsync();
    }

    [Fact] void should_not_wait_for_the_scheduled_flush() => _writesBeforeFlush.ShouldEqual(0);
    [Fact] void should_store_the_partitions_in_two_chunks() => _observerHandledCountsStorage.Received(2).Increment(_observerId, Arg.Any<IReadOnlyCollection<KeyValuePair<Key, IReadOnlyDictionary<EventTypeId, EventCount>>>>());
    [Fact] void should_flush_the_total() => _stateStorage.State.HandledEventCount.ShouldEqual((EventCount)600UL);
    [Fact] void should_write_the_state_once() => _storageStats.Writes.ShouldEqual(1);
}
