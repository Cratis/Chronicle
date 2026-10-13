// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_flushing_an_open_bulk_window;

public class and_writes_are_queued : given.a_sink_with_gated_bulk_writes
{
    BulkFlushResult _firstFlush;
    BulkFlushResult _secondFlush;
    int _writtenBeforeSecondFlush;

    async Task Establish()
    {
        _holdFirstFlush = false;
        await _sink.BeginBulk();
        await Apply(_firstKey, 1, 1UL);
    }

    async Task Because()
    {
        _firstFlush = await _sink.FlushBulk();
        await Apply(_secondKey, 2, 2UL);
        _writtenBeforeSecondFlush = _written.Count;
        _secondFlush = await _sink.FlushBulk();
    }

    [Fact] void should_write_the_queued_operations() => WrittenValuesFor(_firstKey).ShouldContainOnly(1);
    [Fact] void should_keep_buffering_later_writes() => _writtenBeforeSecondFlush.ShouldEqual(1);
    [Fact] void should_write_later_operations_at_the_next_flush() => WrittenValuesFor(_secondKey).ShouldContainOnly(2);
    [Fact] void should_report_an_open_window() => _firstFlush.WindowId.ShouldNotBeNull();
    [Fact] void should_report_the_same_window_id() => _secondFlush.WindowId.ShouldEqual(_firstFlush.WindowId);
    [Fact] void should_not_write_directly() => _directWrites.ShouldBeEmpty();
}
