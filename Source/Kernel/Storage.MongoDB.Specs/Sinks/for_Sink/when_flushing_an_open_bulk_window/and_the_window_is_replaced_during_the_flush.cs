// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_flushing_an_open_bulk_window;

public class and_the_window_is_replaced_during_the_flush : given.a_sink_with_gated_bulk_writes
{
    Exception _error;
    BulkFlushResult _opened;
    BulkFlushResult _replacement;

    async Task Establish()
    {
        await _sink.BeginBulk();
        _opened = await _sink.FlushBulk();
        await Apply(_firstKey, 1, 1UL);
    }

    async Task Because()
    {
        var flushing = _sink.FlushBulk();
        await _firstFlushStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await _sink.BeginBulk();
        _replacement = await _sink.FlushBulk();
        _releaseFirstFlush.SetResult();
        _error = await Catch.Exception(async () => await flushing);
    }

    [Fact] void should_say_the_window_was_replaced() => _error.Message.ShouldContain("replaced");
    [Fact] void should_name_the_previous_window() => _error.Message.ShouldContain(_opened.WindowId!.Value.ToString());
    [Fact] void should_name_the_new_window() => _error.Message.ShouldContain(_replacement.WindowId!.Value.ToString());
    [Fact] void should_warn_that_in_flight_writes_were_discarded() => _logger.Warnings.Count().ShouldEqual(1);
    [Fact] void should_log_the_in_flight_batch_count() => _logger.Warnings.Single().Values["InFlightBatchCount"].ShouldEqual(1);
    [Fact] void should_log_no_queued_writes() => _logger.Warnings.Single().Values["QueuedOperationCount"].ShouldEqual(0);
    [Fact] void should_not_confirm_the_replaced_window() => _error.ShouldBeOfExactType<BulkWindowLost>();
}
