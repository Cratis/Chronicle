// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_applying_changes_immediately;

public class and_a_threshold_flush_is_in_flight : for_Sink.given.a_sink_with_gated_bulk_writes
{
    bool _waitedForBatch;
    FailedPartition[] _failures;

    async Task Establish()
    {
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
        await Apply(_firstKey, 1000, 1000UL);
        var immediate = ((ISink)_sink).ApplyChangesImmediately(_firstKey, Changes(1001), 1001UL, SinkWriteMode.Always);
        _waitedForBatch = !immediate.IsCompleted;
        _releaseFirstFlush.SetResult();
        await threshold;
        _failures = (await immediate).ToArray();
    }

    [Fact] void should_wait_for_the_threshold_flush() => _waitedForBatch.ShouldBeTrue();
    [Fact] void should_write_the_detached_batch_after_the_threshold_batch() => WrittenValuesFor(_firstKey).ShouldEqual(Enumerable.Range(0, 1002).ToArray());
    [Fact] void should_write_the_future_directly() => _directWrites.Count.ShouldEqual(1);
    [Fact] void should_report_no_failures() => _failures.ShouldBeEmpty();
}
