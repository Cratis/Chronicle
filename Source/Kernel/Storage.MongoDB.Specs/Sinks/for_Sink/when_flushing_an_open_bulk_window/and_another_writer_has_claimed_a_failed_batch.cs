// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sinks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_flushing_an_open_bulk_window;

public class and_another_writer_has_claimed_a_failed_batch : given.a_sink_with_gated_bulk_writes
{
    readonly TaskCompletionSource _secondFlushStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _releaseSecondFlush = new(TaskCreationOptions.RunContinuationsAsynchronously);
    Task<IEnumerable<FailedPartition>> _writer;
    FailedPartition[] _writerFailures;
    Exception? _flushError;

    async Task Establish()
    {
        var calls = 0;
        _collection.BulkWriteAsync(Arg.Any<IEnumerable<WriteModel<BsonDocument>>>(), Arg.Any<BulkWriteOptions>(), Arg.Any<CancellationToken>())
            .Returns(async info =>
            {
                var operations = info.ArgAt<IEnumerable<WriteModel<BsonDocument>>>(0).ToArray();
                if (++calls == 1)
                {
                    _firstFlushStarted.SetResult();
                    await _releaseFirstFlush.Task;
                    throw _unexpectedFailure;
                }

                _secondFlushStarted.SetResult();
                await _releaseSecondFlush.Task;
                return (BulkWriteResult<BsonDocument>)new BulkWriteResult<BsonDocument>.Acknowledged(operations.Length, 0, 0, 0, 0, operations, []);
            });
        await _sink.BeginBulk();
        for (var index = 0; index < 999; index++)
        {
            await Apply(_firstKey, index, (ulong)index);
        }

        _writer = Apply(_firstKey, 1000, 1000UL);
        await _firstFlushStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await Apply(_secondKey, 1001, 1001UL);
    }

    async Task Because()
    {
        var flush = _sink.FlushBulk();
        _releaseFirstFlush.SetResult();
        await _secondFlushStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));

        // The size-flush caller receives the failed partitions, but has not recorded them yet.
        _writerFailures = (await _writer).ToArray();
        _releaseSecondFlush.SetResult();
        _flushError = await Catch.Exception(async () => await flush);
    }

    [Fact] void should_deliver_the_failed_partition_to_the_writer() => _writerFailures.Select(failure => failure.EventSourceId).ShouldContainOnly([_firstKey]);
    [Fact] void should_not_confirm_durability_after_the_failure_is_claimed() => _flushError.ShouldNotBeNull();
}
