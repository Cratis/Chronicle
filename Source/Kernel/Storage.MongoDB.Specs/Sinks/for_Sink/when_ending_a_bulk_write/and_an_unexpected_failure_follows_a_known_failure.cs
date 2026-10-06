// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_ending_a_bulk_write;

public class and_an_unexpected_failure_follows_a_known_failure : given.a_sink_with_gated_bulk_writes
{
    Exception _error;

    async Task Establish()
    {
        _failFirstBatch = true;
        _throwLaterBatch = true;
        await _sink.BeginBulk();
        await Apply(_firstKey, 1, 1);
        await Apply(_secondKey, 2, 2);
    }

    async Task Because()
    {
        var ending = _sink.EndBulk();
        await _firstFlushStarted.Task;
        _releaseFirstFlush.SetResult();
        _error = await Catch.Exception(async () => await ending);
    }

    [Fact] void should_preserve_the_known_failure() => (_error is BulkWriteFailed failed && failed.FailedPartitions.Any(partition => partition.EventSourceId == _firstKey && partition.EventSequenceNumber.Value == 1)).ShouldBeTrue();
    [Fact] void should_report_the_partition_with_an_unknown_outcome() => (_error is BulkWriteFailed failed && failed.FailedPartitions.Any(partition => partition.EventSourceId == _secondKey)).ShouldBeTrue();
}
