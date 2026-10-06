// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_ending_a_bulk_write;

public class and_an_unexpected_failure_precedes_a_later_write : given.a_sink_with_gated_bulk_writes
{
    Exception _error;
    Exception _lateError;

    async Task Establish()
    {
        _throwFirstBatch = true;
        await _sink.BeginBulk();
        await Apply(_firstKey, 1, 1);
    }

    async Task Because()
    {
        var ending = _sink.EndBulk();
        await _firstFlushStarted.Task;
        var lateWrite = Apply(_firstKey, 2, 2);
        _releaseFirstFlush.SetResult();
        _error = await Catch.Exception(async () => await ending);
        _lateError = await Catch.Exception(async () => await lateWrite);
        await Apply(_firstKey, 3, 3);
        await _sink.BeginBulk();
        await _sink.EndBulk();
    }

    [Fact] void should_surface_the_failed_partition() => (_error is BulkWriteFailed failed && failed.FailedPartitions.Any(partition => partition.EventSourceId == _firstKey)).ShouldBeTrue();
    [Fact] void should_fail_the_waiting_write_instead_of_acknowledging_orphaned_work() => _lateError.ShouldNotBeNull();
    [Fact] void should_not_flush_an_older_write_after_the_newer_direct_write() => WrittenValuesFor(_firstKey).ShouldNotContain(2);
}
