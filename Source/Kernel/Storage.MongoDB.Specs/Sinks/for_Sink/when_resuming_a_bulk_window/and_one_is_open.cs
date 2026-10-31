// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sinks;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_resuming_a_bulk_window;

public class and_one_is_open : given.a_sink_with_gated_bulk_writes
{
    BulkFlushResult _opened;
    BulkFlushResult _afterResuming;

    async Task Establish()
    {
        _holdFirstFlush = false;
        await _sink.BeginBulk();
        _opened = await _sink.FlushBulk();
        await Apply(_firstKey, 1, 1UL);
    }

    async Task Because()
    {
        await _sink.ResumeBulk();
        await Apply(_secondKey, 2, 2UL);
        _afterResuming = await _sink.FlushBulk();
    }

    [Fact] void should_not_warn() => _logger.Entries.Any(_ => _.Level == LogLevel.Warning).ShouldBeFalse();
    [Fact] void should_keep_the_writes_queued_before_resuming() => WrittenValuesFor(_firstKey).ShouldContainOnly(1);
    [Fact] void should_keep_the_writes_queued_after_resuming() => WrittenValuesFor(_secondKey).ShouldContainOnly(2);
    [Fact] void should_keep_the_same_window() => _afterResuming.WindowId.ShouldEqual(_opened.WindowId);
    [Fact] void should_not_write_directly() => _directWrites.ShouldBeEmpty();
}
