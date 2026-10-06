// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_flushing_a_bulk_write;

public class and_a_preflush_failure_precedes_an_interrupted_join_write : for_Sink.given.a_sink_with_gated_bulk_writes
{
    FailedPartition[] _failures;

    async Task Establish()
    {
        _holdFirstFlush = false;
        _failFirstBatch = true;
        _throwLaterBatch = true;
        await _sink.BeginBulk();
        await Apply(_firstKey, 1, 1);
    }

    async Task Because() => _failures = (await _sink.ApplyChanges(_secondKey, JoinedChanges(2), 2UL)).ToArray();

    [Fact] void should_preserve_both_failures() => _failures.Select(failure => failure.EventSourceId).ShouldContainOnly(_firstKey, _secondKey);
}
