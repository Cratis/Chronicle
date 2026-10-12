// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_applying_changes_immediately;

public class and_a_threshold_flush_already_threw : for_Sink.given.a_sink_with_gated_bulk_writes
{
    Exception? _immediateFailure;
    Exception? _endFailure;

    async Task Establish()
    {
        _holdFirstFlush = false;
        _throwFirstBatch = true;
        await _sink.BeginBulk();
        for (var index = 0; index < 999; index++)
        {
            await Apply(_firstKey, index, (ulong)index);
        }

        await Catch.Exception(async () => await Apply(_firstKey, 999, 999UL));
    }

    async Task Because()
    {
        _immediateFailure = await Catch.Exception(async () => await ((ISink)_sink).ApplyChangesImmediately(_secondKey, Changes(1), 1000UL, SinkWriteMode.Always));
        _endFailure = await Catch.Exception(_sink.EndBulk);
    }

    [Fact] void should_not_rethrow_an_exception_reported_to_its_owner() => _immediateFailure.ShouldBeNull();
    [Fact] void should_not_rethrow_the_reported_exception_at_end_bulk() => _endFailure.ShouldBeNull();
    [Fact] void should_write_the_future_after_the_failed_batch_finished() => _directWrites.Count.ShouldEqual(1);
}
