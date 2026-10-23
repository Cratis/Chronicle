// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_coalescing_writes;

public class and_the_second_handled_count_chunk_fails : given.an_observer_coalescing_writes
{
    const int Partitions = 1100;

    readonly InvalidOperationException _failure = new("storage unavailable");
    readonly List<Key[]> _attemptedChunks = [];
    Exception _error;
    EventCount _totalBeforeRetry;
    EventCount _perTypeBeforeRetry;
    int _writesBeforeRetry;

    async Task Establish()
    {
        var step = JobStepId.New();
        var reports = Enumerable.Range(0, Partitions).Select(i => new HandledEventsReport($"partition-{i}", new Dictionary<EventTypeId, EventCount> { ["event"] = 2UL }, step, (ulong)i)).ToArray();
        await _observer.ReportHandledEventsBatch(step, reports);
        _observerHandledCountsStorage.Increment(_observerId, Arg.Any<IReadOnlyCollection<KeyValuePair<Key, IReadOnlyDictionary<EventTypeId, EventCount>>>>())
            .Returns(call =>
            {
                _attemptedChunks.Add(call.Arg<IReadOnlyCollection<KeyValuePair<Key, IReadOnlyDictionary<EventTypeId, EventCount>>>>().Select(_ => _.Key).ToArray());
                return _attemptedChunks.Count == 2 ? Task.FromException(_failure) : Task.CompletedTask;
            });
    }

    async Task Because()
    {
        _error = await Catch.Exception(_observer.RunWatchdogAsync);
        _totalBeforeRetry = _stateStorage.State.HandledEventCount;
        _perTypeBeforeRetry = _stateStorage.State.HandledEventCountPerEventType[(EventTypeId)"event"];
        _writesBeforeRetry = _storageStats.Writes;
        await _observer.RunWatchdogAsync();
    }

    [Fact] void should_propagate_the_storage_failure() => _error.ShouldBeSame(_failure);
    [Fact] void should_count_only_the_successful_chunk_before_retry() => _totalBeforeRetry.ShouldEqual((EventCount)1024UL);
    [Fact] void should_count_only_the_successful_chunk_per_type_before_retry() => _perTypeBeforeRetry.ShouldEqual((EventCount)1024UL);
    [Fact] void should_not_write_state_for_the_failed_flush() => _writesBeforeRetry.ShouldEqual(0);
    [Fact] void should_retry_the_failed_chunk() => _attemptedChunks[2].ShouldEqual(_attemptedChunks[1]);
    [Fact] void should_not_restore_the_successful_chunk() => _attemptedChunks.Skip(2).SelectMany(_ => _).Intersect(_attemptedChunks[0]).ShouldBeEmpty();
    [Fact] void should_restore_every_unwritten_partition() => _attemptedChunks.Skip(2).SelectMany(_ => _).ShouldEqual(Enumerable.Range(512, Partitions - 512).Select(i => (Key)$"partition-{i}"));
    [Fact] void should_make_only_four_bulk_attempts() => _attemptedChunks.Count.ShouldEqual(4);
    [Fact] void should_not_increment_partitions_individually() => _observerHandledCountsStorage.DidNotReceive().Increment(_observerId, Arg.Any<Key>(), Arg.Any<IReadOnlyDictionary<EventTypeId, EventCount>>());
    [Fact] void should_count_all_partitions_once() => _stateStorage.State.HandledEventCount.ShouldEqual((EventCount)(2UL * Partitions));
    [Fact] void should_count_all_partitions_once_per_type() => _stateStorage.State.HandledEventCountPerEventType[(EventTypeId)"event"].ShouldEqual((EventCount)(2UL * Partitions));
    [Fact] void should_write_state_once_after_retry() => _storageStats.Writes.ShouldEqual(1);
}
