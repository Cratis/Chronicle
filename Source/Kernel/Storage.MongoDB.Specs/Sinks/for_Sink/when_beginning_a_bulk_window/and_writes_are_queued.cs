// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_beginning_a_bulk_window;

public class and_writes_are_queued : given.a_sink_with_gated_bulk_writes
{
    BulkFlushResult _opened;
    BulkFlushResult _afterBeginning;

    async Task Establish()
    {
        _holdFirstFlush = false;
        await _sink.BeginBulk();
        _opened = await _sink.FlushBulk();
        await Apply(_firstKey, 1, 1UL);
    }

    async Task Because()
    {
        await _sink.BeginBulk();
        _afterBeginning = await _sink.FlushBulk();
    }

    [Fact] void should_discard_the_queued_writes() => WrittenValuesFor(_firstKey).ShouldBeEmpty();
    [Fact] void should_open_a_new_window() => _afterBeginning.WindowId.ShouldNotEqual(_opened.WindowId);
}
