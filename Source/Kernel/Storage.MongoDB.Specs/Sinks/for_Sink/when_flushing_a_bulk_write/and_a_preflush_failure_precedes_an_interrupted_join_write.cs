// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_flushing_a_bulk_write;

public class and_a_preflush_failure_precedes_an_interrupted_join_write : for_Sink.given.a_sink_with_gated_bulk_writes
{
    Exception _error;

    async Task Establish()
    {
        _holdFirstFlush = false;
        _failFirstBatch = true;
        _throwLaterBatch = true;
        await _sink.BeginBulk();
        await Apply(_firstKey, 1, 1);
    }

    async Task Because()
    {
        _error = await Catch.Exception(() => _sink.ApplyChanges(_secondKey, JoinedChanges(2), 2UL));
        _throwLaterBatch = false;
        await _sink.EndBulk();
    }

    [Fact] void should_preserve_the_known_preflush_failure() => ((BulkWriteFailed)_error).FailedPartitions.Select(failure => failure.EventSourceId).ShouldContainOnly(_firstKey);
    [Fact] void should_preserve_the_real_interruption() => _error.InnerException.ShouldEqual(_unexpectedFailure);
    [Fact] void should_retry_the_interrupted_join_write() => WrittenValuesFor(_secondKey).ShouldContainOnly(2, 2);
    [Fact] void should_not_write_directly() => _directWrites.ShouldBeEmpty();
}
