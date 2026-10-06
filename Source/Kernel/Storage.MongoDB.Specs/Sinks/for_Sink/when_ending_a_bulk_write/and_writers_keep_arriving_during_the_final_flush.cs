// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_ending_a_bulk_write;

/// <summary>
/// A writer that never pauses must not keep the closing from finishing. The writer stops on its own as soon as the
/// closing has finished; the upper bound only keeps a regression from running forever.
/// </summary>
public class and_writers_keep_arriving_during_the_final_flush : given.a_sink_with_gated_bulk_writes
{
    const int MaximumWrites = 100;

    Task<IEnumerable<FailedPartition>> _ending;
    int _produced;

    async Task Establish()
    {
        await _sink.BeginBulk();
        await Apply(_firstKey, 0, 1);
    }

    async Task Because()
    {
        _ending = _sink.EndBulk();
        await _firstFlushStarted.Task;
        var producer = Produce();
        _releaseFirstFlush.SetResult();
        await _ending;
        await producer;
    }

    async Task Produce()
    {
        while (!_ending.IsCompleted && _produced < MaximumWrites)
        {
            _produced++;
            await Apply(_firstKey, _produced, (ulong)_produced + 1);
        }
    }

    [Fact] void should_finish_closing_before_the_writer_gives_up() => _produced.ShouldBeLessThan(MaximumWrites);
    [Fact] void should_flush_one_bulk_batch() => _batches.Count.ShouldEqual(1);
    [Fact] void should_write_every_update_in_order() => WrittenValuesFor(_firstKey).ShouldEqual(Enumerable.Range(0, _produced + 1).ToArray());
}
