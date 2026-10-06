// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_beginning_a_bulk_write;

public class and_operations_are_already_pending : given.a_sink_with_gated_bulk_writes
{
    async Task Establish()
    {
        _holdFirstFlush = false;
        await _sink.BeginBulk();
        await Apply(_firstKey, 1, 1);
    }

    async Task Because()
    {
        await _sink.BeginBulk();
        await _sink.EndBulk();
    }

    [Fact] void should_preserve_the_pending_update() => WrittenCountFor(_firstKey).ShouldEqual(1);
}
