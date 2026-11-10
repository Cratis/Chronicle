// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Imports;

namespace Cratis.Chronicle.Storage.InMemory.Imports.for_ImportStorage;

public class when_getting_chunk_summaries : given.an_import_storage
{
    ImportChunkSummary[] _summaries;

    async Task Because()
    {
        await _storage.AddChunk(Chunk(_importId, 2, 1));
        await _storage.AddChunk(Chunk(_importId, 0, 3, rejected: 1));
        await _storage.AddChunk(Chunk(_otherImportId, 1, 4));
        await _storage.AddChunk(Chunk(_importId, 1, 2));
        _summaries = (await FreshStorage().GetChunkSummaries(_importId)).ToArray();
    }

    [Fact] void should_return_only_the_chunks_of_the_import_ordered_by_index() => _summaries.Select(_ => _.ChunkIndex).ShouldEqual([0, 1, 2]);
    [Fact] void should_count_received_staged_and_rejected_events() => (_summaries[0].ChunkIndex, _summaries[0].FirstEventIndex, _summaries[0].EventsReceived, _summaries[0].EventsStaged, _summaries[0].EventsRejected).ShouldEqual((0, 0L, 3, 2, 1));
}
