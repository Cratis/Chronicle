// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_flushing_a_bulk_write;

public class and_an_unknown_threshold_failure_has_concurrent_writes : for_Sink.given.a_sink_with_gated_bulk_writes
{
    Exception _error;
    bool _accepted;
    bool _cached;

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
        var flushing = Apply(_firstKey, 999, 999);
        await _firstFlushStarted.Task;
        _accepted = !(await Apply(_secondKey, 1000, 1000)).Any();
        _releaseFirstFlush.SetResult();
        _error = await Catch.Exception(async () => await flushing);
        await Apply(_secondKey, 1001, 1001);
        _cached = await _sink.FindOrDefault(_secondKey) is not null;
        await _sink.EndBulk();
    }

    [Fact] void should_rethrow_the_original_failure() => _error.ShouldEqual(_unexpectedFailure);
    [Fact] void should_accept_the_other_writer() => _accepted.ShouldBeTrue();
    [Fact] void should_keep_the_bulk_cache() => _cached.ShouldBeTrue();
    [Fact] void should_not_allow_direct_writes() => _directWrites.ShouldBeEmpty();
    [Fact] void should_retry_the_failed_batch_before_concurrent_writes() => _batches[1].Length.ShouldEqual(1002);
    [Fact] void should_preserve_other_writers_order() => WrittenValuesFor(_secondKey).SequenceEqual([1000, 1001]).ShouldBeTrue();
    [Fact] void should_retain_the_failed_operations() => _batches[1].Take(1000).SequenceEqual(_batches[0]).ShouldBeTrue();
}
