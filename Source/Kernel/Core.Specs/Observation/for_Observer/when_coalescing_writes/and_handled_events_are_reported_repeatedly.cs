// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Properties;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_coalescing_writes;

public class and_handled_events_are_reported_repeatedly : given.an_observer_coalescing_writes
{
    const string TypeA = "a";
    const string TypeB = "b";

    Key _partition;
    Key _otherPartition;
    int _writesBeforeFlush;
    int _incrementsBeforeFlush;
    JobStepId _step;

    void Establish()
    {
        _partition = new Key("partition", ArrayIndexers.NoIndexers);
        _otherPartition = new Key("other", ArrayIndexers.NoIndexers);
        _step = JobStepId.New();
        _storageStats.ResetCounts();
        _observerHandledCountsStorage.ClearReceivedCalls();
    }

    async Task Because()
    {
        for (var i = 0; i < 10; i++)
        {
            await _observer.ReportHandledEvents(_partition, new Dictionary<EventTypeId, EventCount> { [TypeA] = 2UL, [TypeB] = 1UL }, _step, (ulong)i);
        }

        await _observer.ReportHandledEvents(_otherPartition, new Dictionary<EventTypeId, EventCount> { [TypeA] = 5UL }, _step, 0UL);

        _writesBeforeFlush = _storageStats.Writes;
        _incrementsBeforeFlush = _observerHandledCountsStorage.ReceivedCalls().Count(_ => _.GetMethodInfo().Name == nameof(_observerHandledCountsStorage.Increment));
        await _observer.RunWatchdogAsync();
    }

    [Fact] void should_not_write_while_the_reports_are_coalesced() => _writesBeforeFlush.ShouldEqual(0);
    [Fact] void should_not_increment_storage_while_the_reports_are_coalesced() => _incrementsBeforeFlush.ShouldEqual(0);
    [Fact] void should_increment_the_partition_once_with_the_sum_of_the_reports() => _observerHandledCountsStorage.Received(1).Increment(
        _observerId,
        _partition,
        Arg.Is<IReadOnlyDictionary<EventTypeId, EventCount>>(counts => counts[TypeA] == 20UL && counts[TypeB] == 10UL));
    [Fact] void should_increment_the_other_partition_once() => _observerHandledCountsStorage.Received(1).Increment(_observerId, _otherPartition, Arg.Any<IReadOnlyDictionary<EventTypeId, EventCount>>());
    [Fact] void should_write_the_state_once_for_all_of_them() => _storageStats.Writes.ShouldEqual(1);
    [Fact] void should_have_the_exact_total_in_state() => _stateStorage.State.HandledEventCount.Value.ShouldEqual(35UL);
}
