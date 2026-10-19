// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_ending_a_bulk_write;

/// <summary>
/// A final flush that throws leaves the outcome of the batch unknown. Every partition the batch held is reported as
/// failed, whichever writer queued it; the batch is neither retained nor sent again, and the sink still leaves bulk
/// mode so later writes go straight to the collection.
/// </summary>
public class and_the_final_flush_throws : given.a_sink_with_gated_bulk_writes
{
    FailedPartition[] _failures;

    async Task Establish()
    {
        _holdFirstFlush = false;
        _throwFirstBatch = true;
        await _sink.BeginBulk();
        await Apply(_firstKey, 1, 1);
        await Apply(_secondKey, 1, 2);
        await Apply(_firstKey, 3, 3);
    }

    async Task Because()
    {
        _failures = (await _sink.EndBulk()).ToArray();
        await Apply(_firstKey, 2, 4);
        await _sink.EndBulk();
    }

    [Fact] void should_report_every_partition_of_the_batch() => _failures.Select(failure => failure.EventSourceId).ShouldContainOnly([_firstKey, _secondKey]);
    [Fact] void should_report_the_earliest_event_of_each_partition() => _failures.Select(failure => failure.EventSequenceNumber.Value).ShouldContainOnly([1UL, 2UL]);
    [Fact] void should_not_send_the_failed_batch_again() => _batches.Count.ShouldEqual(1);
    [Fact] void should_write_later_changes_directly() => WrittenValuesFor(_firstKey).ShouldEqual([2]);
}
