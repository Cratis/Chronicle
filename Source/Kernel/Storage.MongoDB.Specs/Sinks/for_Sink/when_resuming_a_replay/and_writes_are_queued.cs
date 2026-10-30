// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Storage.ReadModels;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_resuming_a_replay;

public class and_writes_are_queued : given.a_sink_with_gated_bulk_writes
{
    readonly ReplayContext _context = new(new ReadModelType("bulk-model", ReadModelGeneration.First), "bulk", "revert-bulk", DateTimeOffset.UtcNow);
    BulkFlushResult _opened;
    BulkFlushResult _afterResuming;

    async Task Establish()
    {
        _holdFirstFlush = false;
        await _sink.ResumeReplay(_context);
        _opened = await _sink.FlushBulk();
        await Apply(_firstKey, 1, 1UL);
    }

    async Task Because()
    {
        await _sink.ResumeReplay(_context);
        await Apply(_secondKey, 2, 2UL);
        _afterResuming = await _sink.FlushBulk();
    }

    [Fact] void should_keep_the_writes_queued_before_resuming() => WrittenValuesFor(_firstKey).ShouldContainOnly(1);
    [Fact] void should_keep_the_writes_queued_after_resuming() => WrittenValuesFor(_secondKey).ShouldContainOnly(2);
    [Fact] void should_keep_the_same_window() => _afterResuming.WindowId.ShouldEqual(_opened.WindowId);
    [Fact] void should_not_write_directly() => _directWrites.ShouldBeEmpty();
}
