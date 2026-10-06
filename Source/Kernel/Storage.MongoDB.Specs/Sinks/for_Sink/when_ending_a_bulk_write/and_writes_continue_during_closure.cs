// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_ending_a_bulk_write;

public class and_writes_continue_during_closure : given.a_sink_with_gated_bulk_writes
{
    bool _producerCompletedDuringFlush;

    async Task Establish()
    {
        await _sink.BeginBulk();
        await Apply(_firstKey, 1, 1);
    }

    async Task Because()
    {
        var ending = _sink.EndBulk();
        await _firstFlushStarted.Task;
        var producer = Produce();
        _producerCompletedDuringFlush = producer.IsCompleted;
        _releaseFirstFlush.SetResult();
        await ending;
        await producer;
    }

    async Task Produce()
    {
        for (var count = 2; count <= 4; count++)
        {
            await Apply(_firstKey, count, (ulong)count);
        }
    }

    [Fact] void should_stop_admitting_buffered_writes_while_closing() => _producerCompletedDuringFlush.ShouldBeFalse();
    [Fact] void should_finish_with_one_bulk_batch() => _batches.Count.ShouldEqual(1);
    [Fact] void should_write_late_events_directly() => _directWrites.Count.ShouldEqual(3);
    [Fact] void should_preserve_event_order() => WrittenValuesFor(_firstKey).ShouldContainOnly(1, 2, 3, 4);
}
