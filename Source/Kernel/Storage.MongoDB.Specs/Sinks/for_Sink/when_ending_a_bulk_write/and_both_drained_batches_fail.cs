// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_ending_a_bulk_write;

public class and_both_drained_batches_fail : given.a_sink_with_gated_bulk_writes
{
    FailedPartition[] _failures;

    async Task Establish()
    {
        _failFirstBatch = true;
        _failLaterBatch = true;
        await _sink.BeginBulk();
        await Apply(_firstKey, 1, 1);
    }

    async Task Because()
    {
        var ending = _sink.EndBulk();
        await _firstFlushStarted.Task;
        try
        {
            await Apply(_secondKey, 2, 2);
        }
        finally
        {
            _releaseFirstFlush.SetResult();
        }

        _failures = (await ending).ToArray();
    }

    [Fact] void should_return_failures_from_both_batches() => _failures.Select(failure => failure.EventSourceId).ShouldContainOnly(_firstKey, _secondKey);
}
