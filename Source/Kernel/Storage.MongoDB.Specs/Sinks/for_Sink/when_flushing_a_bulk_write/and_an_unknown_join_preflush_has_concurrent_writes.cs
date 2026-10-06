// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_flushing_a_bulk_write;

public class and_an_unknown_join_preflush_has_concurrent_writes : for_Sink.given.a_sink_with_gated_bulk_writes
{
    Exception _error;

    async Task Establish()
    {
        _throwFirstBatch = true;
        await _sink.BeginBulk();
        await Apply(_firstKey, 1, 1);
    }

    async Task Because()
    {
        var joining = _sink.ApplyChanges(_secondKey, JoinedChanges(3), 3UL);
        await _firstFlushStarted.Task;
        await Apply(_firstKey, 2, 2);
        _releaseFirstFlush.SetResult();
        _error = await Catch.Exception(async () => await joining);
        await _sink.EndBulk();
        await _sink.ApplyChanges(_secondKey, JoinedChanges(3), 3UL);
    }

    [Fact] void should_rethrow_the_original_failure() => _error.ShouldEqual(_unexpectedFailure);
    [Fact] void should_retain_the_preflush_and_concurrent_operations() => WrittenValuesFor(_firstKey).ShouldContainOnly(1, 1, 2);
    [Fact] void should_not_acknowledge_the_unqueued_join() => WrittenValuesFor(_secondKey).ShouldContainOnly(3);
}
