// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_ending_a_bulk_write;

public class and_a_threshold_flush_is_in_flight : given.a_sink_with_gated_bulk_writes
{
    bool _closedBeforeFlushCompleted;

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
        var ending = _sink.EndBulk();
        _closedBeforeFlushCompleted = ending.IsCompleted;
        _releaseFirstFlush.SetResult();
        await thresholdFlush;
        await ending;
    }

    [Fact] void should_wait_for_the_detached_batch_before_closing() => _closedBeforeFlushCompleted.ShouldBeFalse();
}
