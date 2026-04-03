// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_AppendedEventsQueue.when_enqueuing;

/// <summary>
/// Regression spec for the race condition where HandlePartitioned grouped events by
/// partition before delivering them. This caused the observer's NextEventSequenceNumber
/// to advance past lower-numbered events from the other partition, dropping them.
/// </summary>
public class interleaved_events_across_partitions_with_single_subscriber : given.a_single_subscriber_with_interleaved_events_across_two_partitions
{
    async Task Because()
    {
        await _queue.Enqueue(_allEvents);
        await _queue.AwaitQueueDepletion();
    }

    [Fact]
    void should_deliver_events_to_both_partitions() =>
        _handledEventsPerPartition.Count.ShouldEqual(2);

    [Fact]
    void should_deliver_all_four_events_for_partition_a() =>
        _handledEventsPerPartition[_partitionA]
            .SelectMany(h => h.Events)
            .Count()
            .ShouldEqual(4);

    [Fact]
    void should_deliver_all_two_events_for_partition_b() =>
        _handledEventsPerPartition[_partitionB]
            .SelectMany(h => h.Events)
            .Count()
            .ShouldEqual(2);

    [Fact]
    void should_deliver_all_six_events_in_total() =>
        _handledEventsPerPartition
            .SelectMany(p => p.Value)
            .SelectMany(h => h.Events)
            .Count()
            .ShouldEqual(6);

    [Fact]
    void should_deliver_partition_a_events_in_sequence_order() =>
        _handledEventsPerPartition[_partitionA]
            .SelectMany(h => h.Events)
            .Select(e => (ulong)e.Context.SequenceNumber)
            .ShouldContainOnly([0UL, 1UL, 4UL, 5UL]);

    [Fact]
    void should_deliver_partition_b_events_in_sequence_order() =>
        _handledEventsPerPartition[_partitionB]
            .SelectMany(h => h.Events)
            .Select(e => (ulong)e.Context.SequenceNumber)
            .ShouldContainOnly([2UL, 3UL]);

    [Fact]
    void should_deliver_first_batch_of_partition_a_before_partition_b() =>
        _handledEventsPerPartition[_partitionA][0]
            .Events
            .Select(e => (ulong)e.Context.SequenceNumber)
            .ShouldContainOnly([0UL, 1UL]);

    [Fact]
    void should_deliver_second_batch_of_partition_a_after_partition_b() =>
        _handledEventsPerPartition[_partitionA][^1]
            .Events
            .Select(e => (ulong)e.Context.SequenceNumber)
            .ShouldContainOnly([4UL, 5UL]);
}
