// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sinks;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_beginning_a_bulk_window;

public class and_one_is_open : given.a_sink_with_gated_bulk_writes
{
    BulkFlushResult _previous;
    BulkFlushResult _current;

    async Task Establish()
    {
        await _sink.BeginBulk();
        _previous = await _sink.FlushBulk();
    }

    async Task Because()
    {
        await _sink.BeginBulk();
        _current = await _sink.FlushBulk();
    }

    [Fact] void should_not_warn() => _logger.Entries.Any(_ => _.Level == LogLevel.Warning).ShouldBeFalse();
    [Fact] void should_report_a_new_window_id() => (_current.WindowId != _previous.WindowId).ShouldBeTrue();
    [Fact] void should_report_an_open_window() => _current.WindowId.ShouldNotBeNull();
}
