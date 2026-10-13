// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_flushing_an_open_bulk_window;

public class and_the_write_outcome_is_unknown : given.a_sink_with_gated_bulk_writes
{
    BulkFlushResult _result;
    BulkFlushResult _next;

    async Task Establish()
    {
        _holdFirstFlush = false;
        _throwFirstBatch = true;
        await _sink.BeginBulk();
        await Apply(_firstKey, 1, 1UL);
    }

    async Task Because()
    {
        _result = await _sink.FlushBulk();
        _next = await _sink.FlushBulk();
    }

    [Fact] void should_report_the_failed_partition() => _result.FailedPartitions.Select(failure => failure.EventSourceId).ShouldContainOnly(_firstKey);
    [Fact] void should_report_the_failure_only_once() => _next.FailedPartitions.ShouldBeEmpty();
    [Fact] void should_keep_the_window_open() => _next.WindowId.ShouldEqual(_result.WindowId);
}
