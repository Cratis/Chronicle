// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_flushing_a_bulk_write;

public class and_another_partition_writes_during_a_threshold_flush : for_Sink.given.a_sink_with_gated_bulk_writes
{
    bool _completedBeforeRelease;

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
        var thresholdFlush = Apply(_firstKey, 999, 999);
        await _firstFlushStarted.Task;
        var otherPartition = Apply(_secondKey, 1000, 1000);
        _completedBeforeRelease = otherPartition.IsCompleted;
        _releaseFirstFlush.SetResult();
        await thresholdFlush;
        await otherPartition;
        await _sink.EndBulk();
    }

    [Fact] void should_admit_other_partitions_before_closure_starts() => _completedBeforeRelease.ShouldBeTrue();
    [Fact] void should_flush_the_other_partition_on_closure() => WrittenCountFor(_secondKey).ShouldEqual(1);
}
