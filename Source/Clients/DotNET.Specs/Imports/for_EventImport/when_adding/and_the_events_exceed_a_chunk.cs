// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Imports.for_EventImport.when_adding;

public class and_the_events_exceed_a_chunk : given.an_event_import
{
    async Task Because()
    {
        await Import.Add(Events(7));
        await Import.Complete();
    }

    [Fact] void should_cut_them_into_chunks_within_the_server_limit() => _chunks.OrderBy(_ => _.ChunkIndex).Select(_ => _.Events.Count()).ShouldEqual([3, 3, 1]);
    [Fact] void should_number_the_chunks_from_zero() => _chunks.Select(_ => _.ChunkIndex).Order().ShouldEqual([0, 1, 2]);
    [Fact] void should_tell_each_chunk_where_its_events_start() => _chunks.OrderBy(_ => _.ChunkIndex).Select(_ => _.FirstEventIndex).ShouldEqual([0L, 3L, 6L]);
    [Fact] void should_complete_with_the_counts_it_sent() => (_completed!.ExpectedChunks, _completed.ExpectedEvents).ShouldEqual((3, 7L));
}
