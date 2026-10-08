// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_ending_a_bulk_write;

/// <summary>
/// A writer that never pauses must not keep the closing from finishing. While the final flush is in flight the writer
/// is held on its first write instead of spinning; it stops when the spec tells it to, after the closing has finished.
/// The upper bound only keeps a regression from running forever.
/// </summary>
public class and_writers_keep_arriving_during_the_final_flush : given.a_sink_with_gated_bulk_writes
{
    const int MaximumWrites = 100;

    readonly TaskCompletionSource _stopProducing = new(TaskCreationOptions.RunContinuationsAsynchronously);
    int _produced;
    int _producedWhileFlushing;
    bool _producerCompletedWhileFlushing;
    bool _endingCompleted;

    async Task Establish()
    {
        await _sink.BeginBulk();
        await Apply(_firstKey, 0, 1);
    }

    async Task Because()
    {
        var ending = _sink.EndBulk();
        await _firstFlushStarted.Task;
        var producer = Produce();
        _producedWhileFlushing = _produced;
        _producerCompletedWhileFlushing = producer.IsCompleted;
        _releaseFirstFlush.SetResult();
        await ending;
        _endingCompleted = ending.IsCompletedSuccessfully;
        _stopProducing.SetResult();
        await producer;
    }

    async Task Produce()
    {
        while (!_stopProducing.Task.IsCompleted && _produced < MaximumWrites)
        {
            _produced++;
            await Apply(_firstKey, _produced, (ulong)_produced + 1);
        }
    }

    [Fact] void should_hold_the_writer_on_its_first_write_while_closing() => _producedWhileFlushing.ShouldEqual(1);
    [Fact] void should_not_let_the_writer_finish_while_closing() => _producerCompletedWhileFlushing.ShouldBeFalse();
    [Fact] void should_complete_the_closing() => _endingCompleted.ShouldBeTrue();
    [Fact] void should_flush_one_bulk_batch() => _batches.Count.ShouldEqual(1);
    [Fact] void should_write_every_update_in_order() => WrittenValuesFor(_firstKey).ShouldEqual(Enumerable.Range(0, _produced + 1).ToArray());
}
