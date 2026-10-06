// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_ending_a_bulk_write;

/// <summary>
/// A final flush that throws leaves the outcome of the batch unknown. The failure reaches the caller, the batch is
/// neither retained nor sent again, and the sink still leaves bulk mode so later writes go straight to the collection.
/// </summary>
public class and_the_final_flush_throws : given.a_sink_with_gated_bulk_writes
{
    Exception _error;

    async Task Establish()
    {
        _holdFirstFlush = false;
        _throwFirstBatch = true;
        await _sink.BeginBulk();
        await Apply(_firstKey, 1, 1);
    }

    async Task Because()
    {
        _error = await Catch.Exception(_sink.EndBulk);
        await Apply(_firstKey, 2, 2);
        await _sink.EndBulk();
    }

    [Fact] void should_fail_with_the_flush_failure() => _error.ShouldEqual(_unexpectedFailure);
    [Fact] void should_not_send_the_failed_batch_again() => _batches.Count.ShouldEqual(1);
    [Fact] void should_write_later_changes_directly() => WrittenValuesFor(_firstKey).ShouldEqual([2]);
}
