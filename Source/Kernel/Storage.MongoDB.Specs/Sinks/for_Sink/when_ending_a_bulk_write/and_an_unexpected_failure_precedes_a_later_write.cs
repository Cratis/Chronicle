// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_ending_a_bulk_write;

public class and_an_unexpected_failure_precedes_a_later_write : given.a_sink_with_gated_bulk_writes
{
    Exception _error;
    Exception _lateError;
    IEnumerable<FailedPartition> _concurrentFailures;

    async Task Establish()
    {
        _throwFirstBatch = true;
        await _sink.BeginBulk();
        await Apply(_firstKey, 1, 1);
    }

    async Task Because()
    {
        var ending = _sink.EndBulk();
        await _firstFlushStarted.Task;
        var concurrentEnding = _sink.EndBulk();
        var lateWrite = Apply(_firstKey, 2, 2);
        _releaseFirstFlush.SetResult();
        _error = await Catch.Exception(async () => await ending);
        _concurrentFailures = await concurrentEnding;
        _lateError = await Catch.Exception(async () => await lateWrite);
        await Apply(_firstKey, 3, 3);
        await _sink.EndBulk();
    }

    [Fact] void should_rethrow_the_original_failure() => _error.ShouldEqual(_unexpectedFailure);
    [Fact] void should_reopen_bulk_admission_for_the_waiting_writer() => _lateError.ShouldBeNull();
    [Fact] void should_not_report_the_owners_failure_again() => _concurrentFailures.ShouldBeEmpty();
    [Fact] void should_not_allow_direct_writes() => _directWrites.ShouldBeEmpty();
    [Fact] void should_retry_every_accepted_write_in_order() => WrittenValuesFor(_firstKey).SequenceEqual([1, 1, 2, 3]).ShouldBeTrue();
}
