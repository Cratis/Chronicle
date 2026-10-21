// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_flushing_an_open_bulk_window;

public class and_the_window_is_replaced_during_the_flush : given.a_sink_with_gated_bulk_writes
{
    Exception _error;

    async Task Establish()
    {
        await _sink.BeginBulk();
        await Apply(_firstKey, 1, 1UL);
    }

    async Task Because()
    {
        var flushing = _sink.FlushBulk();
        await _firstFlushStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await _sink.BeginBulk();
        _releaseFirstFlush.SetResult();
        _error = await Catch.Exception(async () => await flushing);
    }

    [Fact] void should_not_confirm_the_replaced_window() => _error.ShouldBeOfExactType<BulkWindowLost>();
}
