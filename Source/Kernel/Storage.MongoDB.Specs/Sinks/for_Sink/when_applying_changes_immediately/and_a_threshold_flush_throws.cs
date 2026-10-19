// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_applying_changes_immediately;

public class and_a_threshold_flush_throws : for_Sink.given.a_sink_with_gated_bulk_writes
{
    FailedPartition[] _waiterFailures;
    FailedPartition[] _thresholdFailures = [];
    Exception? _thresholdFailure;
    bool _waitedForBatch;

    async Task Establish()
    {
        _throwFirstBatch = true;
        await _sink.BeginBulk();
        for (var index = 0; index < 999; index++)
        {
            await Apply(_firstKey, index, (ulong)index);
        }
    }

    async Task Because()
    {
        var threshold = Apply(_firstKey, 999, 999UL);
        await _firstFlushStarted.Task.WaitAsync(TimeSpan.FromSeconds(10));
        var immediate = ((ISink)_sink).ApplyChangesImmediately(_secondKey, Changes(1), 1000UL, SinkWriteMode.Always);
        _waitedForBatch = !immediate.IsCompleted;
        _releaseFirstFlush.SetResult();
        _thresholdFailure = await Catch.Exception(async () => _thresholdFailures = (await threshold).ToArray());
        _waiterFailures = (await immediate).ToArray();
    }

    [Fact] void should_wait_for_the_in_flight_batch() => _waitedForBatch.ShouldBeTrue();
    [Fact] void should_not_throw_to_the_writer_that_filled_the_batch() => _thresholdFailure.ShouldBeNull();
    [Fact] void should_report_the_unknown_outcome_exactly_once() => (_waiterFailures.Length + _thresholdFailures.Length).ShouldEqual(1);
    [Fact] void should_write_the_independent_future_after_the_batch_finished() => _directWrites.Count.ShouldEqual(1);
}
