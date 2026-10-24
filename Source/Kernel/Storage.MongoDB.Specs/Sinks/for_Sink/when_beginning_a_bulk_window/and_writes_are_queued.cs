// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sinks;
using Microsoft.Extensions.Logging;

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

    [Fact] void should_warn_that_the_open_window_was_replaced() => _logger.Entries.Count(_ => _.Level == LogLevel.Warning).ShouldEqual(1);
    [Fact] void should_log_the_discarded_write_count() => _logger.Entries.Single().Values["QueuedOperationCount"].ShouldEqual(1);
    [Fact] void should_log_the_previous_window() => _logger.Entries.Single().Values["PreviousWindow"].ShouldEqual(_opened.WindowId!.Value);
    [Fact] void should_log_the_new_window() => _logger.Entries.Single().Values["Window"].ShouldEqual(_afterBeginning.WindowId!.Value);
    [Fact] void should_log_the_mode() => _logger.Entries.Single().Values["Mode"].ShouldEqual("catch-up");
    [Fact] void should_log_no_in_flight_batches() => _logger.Entries.Single().Values["InFlightBatchCount"].ShouldEqual(0);
    [Fact] void should_not_log_document_keys() => _logger.Entries.Any(_ => _.Message.Contains(_firstKey.Value.ToString()!)).ShouldBeFalse();
    [Fact] void should_discard_the_queued_writes() => WrittenValuesFor(_firstKey).ShouldBeEmpty();
    [Fact] void should_open_a_new_window() => _afterBeginning.WindowId.ShouldNotEqual(_opened.WindowId);
}
