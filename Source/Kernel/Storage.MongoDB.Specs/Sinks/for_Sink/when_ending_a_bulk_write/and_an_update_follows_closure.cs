// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_ending_a_bulk_write;

public class and_an_update_follows_closure : given.a_sink_with_gated_bulk_writes
{
    async Task Establish()
    {
        _holdFirstFlush = false;
        await _sink.BeginBulk();
        await Apply(_firstKey, 1, 1);
    }

    async Task Because()
    {
        await _sink.EndBulk();
        await Apply(_secondKey, 2, 2);
    }

    [Fact] void should_write_the_later_update_directly() => _directWrites.Count.ShouldEqual(1);
    [Fact] void should_not_queue_the_later_update() => _batches.Count.ShouldEqual(1);
}
