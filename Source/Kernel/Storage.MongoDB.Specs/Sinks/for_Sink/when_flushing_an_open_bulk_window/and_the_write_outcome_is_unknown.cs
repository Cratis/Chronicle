// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_flushing_an_open_bulk_window;

public class and_the_write_outcome_is_unknown : given.a_sink_with_gated_bulk_writes
{
    BulkFlushResult _result;
    Exception? _nextError;
    BulkFlushResult _newWindow;

    async Task Establish()
    {
        _holdFirstFlush = false;
        _throwFirstBatch = true;
        await _sink.BeginBulk();
        await Apply(_firstKey, 1, 1UL);
    }

    async Task Because()
    {
        _result = await _sink.FlushBulk();
        _nextError = await Catch.Exception(_sink.FlushBulk);
        await _sink.BeginBulk();
        _newWindow = await _sink.FlushBulk();
    }

    [Fact] void should_report_the_failed_partition() => _result.FailedPartitions.Select(failure => failure.EventSourceId).ShouldContainOnly(_firstKey);
    [Fact] void should_reject_durability_after_the_outcome_is_claimed() => _nextError.ShouldBeOfExactType<BulkWindowHasFailedWrites>();
    [Fact] void should_allow_a_new_window_to_confirm_durability() => _newWindow.FailedPartitions.ShouldBeEmpty();
    [Fact] void should_replace_the_failed_window_identity() => (_newWindow.WindowId != _result.WindowId).ShouldBeTrue();
}
