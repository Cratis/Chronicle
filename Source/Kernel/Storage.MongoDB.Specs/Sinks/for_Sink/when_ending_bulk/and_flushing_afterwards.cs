// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_ending_bulk;

public class and_flushing_afterwards : given.a_sink_with_gated_bulk_writes
{
    BulkFlushResult _result;

    async Task Establish()
    {
        _holdFirstFlush = false;
        await _sink.BeginBulk();
        await _sink.EndBulk();
    }

    async Task Because() => _result = await _sink.FlushBulk();

    [Fact] void should_not_report_a_window() => _result.WindowId.ShouldBeNull();
}
