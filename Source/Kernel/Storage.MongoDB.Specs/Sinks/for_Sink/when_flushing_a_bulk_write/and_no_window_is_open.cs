// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_flushing_a_bulk_write;

public class and_no_window_is_open : for_Sink.given.a_sink_with_gated_bulk_writes
{
    BulkFlushResult _result;

    void Establish() => _holdFirstFlush = false;

    async Task Because() => _result = await _sink.FlushBulk();

    [Fact] void should_not_report_a_window() => _result.WindowId.ShouldBeNull();
}
