// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_resuming_a_bulk_window;

public class and_none_is_open : given.a_sink_with_gated_bulk_writes
{
    BulkFlushResult _result;

    async Task Establish()
    {
        _holdFirstFlush = false;
        await _sink.BeginBulk();
        await _sink.EndBulk();
    }

    async Task Because()
    {
        await _sink.ResumeBulk();
        await Apply(_firstKey, 1, 1UL);
        _result = await _sink.FlushBulk();
    }

    [Fact] void should_open_a_window() => _result.WindowId.ShouldNotBeNull();
    [Fact] void should_buffer_writes_in_it() => WrittenValuesFor(_firstKey).ShouldContainOnly(1);
    [Fact] void should_not_write_directly() => _directWrites.ShouldBeEmpty();
}
