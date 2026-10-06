// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_ending_a_bulk_write;

public class and_an_update_arrives_during_the_final_flush : given.a_sink_with_gated_bulk_writes
{
    async Task Establish()
    {
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

        await ending;
    }

    [Fact] void should_write_the_update_accepted_during_the_flush() => WrittenCountFor(_secondKey).ShouldEqual(1);
    [Fact] void should_write_the_original_update() => WrittenCountFor(_firstKey).ShouldEqual(1);
}
