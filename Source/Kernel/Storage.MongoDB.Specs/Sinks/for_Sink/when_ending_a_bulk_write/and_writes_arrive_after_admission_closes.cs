// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_ending_a_bulk_write;

public class and_writes_arrive_after_admission_closes : given.a_sink_with_gated_bulk_writes
{
    bool _completedDuringFlush;
    int _directWritesDuringFlush;

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
        _completedDuringFlush = producer.IsCompleted;
        _directWritesDuringFlush = _directWrites.Count;
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

    [Fact] void should_hold_the_writes_back_while_closing() => _completedDuringFlush.ShouldBeFalse();
    [Fact] void should_not_write_directly_while_the_flush_is_in_flight() => _directWritesDuringFlush.ShouldEqual(0);
    [Fact] void should_flush_one_bulk_batch() => _batches.Count.ShouldEqual(1);
    [Fact] void should_write_the_later_writes_directly() => _directWrites.Count.ShouldEqual(3);
    [Fact] void should_write_every_update_in_order() => WrittenValuesFor(_firstKey).ShouldEqual([1, 2, 3, 4]);
}
