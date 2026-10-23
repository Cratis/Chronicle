// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Properties;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_coalescing_writes;

/// <summary>
/// The report interleaves with the observer's other work, so it never waits for a flush - not even when enough
/// partitions are pending to bring the flush forward. Every count still reaches storage exactly once.
/// </summary>
public class and_many_partitions_are_reported : given.an_observer_coalescing_writes
{
    const int Partitions = 600;
    const string TypeA = "a";

    int _writesWhileReporting;

    void Establish()
    {
        _storageStats.ResetCounts();
        _observerHandledCountsStorage.ClearReceivedCalls();
    }

    async Task Because()
    {
        for (var i = 0; i < Partitions; i++)
        {
            await _observer.ReportHandledEvents(new Key($"partition-{i}", ArrayIndexers.NoIndexers), new Dictionary<EventTypeId, EventCount> { [TypeA] = 1UL }, JobStepId.New(), (ulong)i);
        }

        _writesWhileReporting = _storageStats.Writes;
        await _observer.RunWatchdogAsync();
    }

    [Fact] void should_not_write_while_reporting() => _writesWhileReporting.ShouldEqual(0);
    [Fact] void should_write_two_chunks() => _observerHandledCountsStorage.Received(2).Increment(_observerId, Arg.Any<IReadOnlyCollection<KeyValuePair<Key, IReadOnlyDictionary<EventTypeId, EventCount>>>>());
    [Fact] void should_fill_the_first_chunk() => _observerHandledCountsStorage.Received(1).Increment(_observerId, Arg.Is<IReadOnlyCollection<KeyValuePair<Key, IReadOnlyDictionary<EventTypeId, EventCount>>>>(counts => counts.Count == 512));
    [Fact] void should_write_the_remaining_partitions() => _observerHandledCountsStorage.Received(1).Increment(_observerId, Arg.Is<IReadOnlyCollection<KeyValuePair<Key, IReadOnlyDictionary<EventTypeId, EventCount>>>>(counts => counts.Count == 88));
    [Fact] void should_not_increment_partitions_individually() => _observerHandledCountsStorage.DidNotReceive().Increment(_observerId, Arg.Any<Key>(), Arg.Any<IReadOnlyDictionary<EventTypeId, EventCount>>());
    [Fact] void should_have_the_exact_per_type_total() => _stateStorage.State.HandledEventCountPerEventType[TypeA].Value.ShouldEqual((ulong)Partitions);
    [Fact] void should_write_state_once() => _storageStats.Writes.ShouldEqual(1);
    [Fact] void should_have_the_exact_total_in_state() => _stateStorage.State.HandledEventCount.Value.ShouldEqual((ulong)Partitions);
}
