// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Imports.for_ImportStorage;

public class when_removing_one_chunk : given.an_import_storage
{
    int[] _remaining;

    async Task Because()
    {
        await _storage.AddChunk(Chunk(_importId, 0, 2));
        await _storage.AddChunk(Chunk(_importId, 1, 2));
        await _storage.RemoveChunk(_importId, 0);
        _remaining = (await FreshStorage().GetChunkSummaries(_importId)).Select(_ => _.ChunkIndex).ToArray();
    }

    [Fact] void should_remove_only_that_chunk() => _remaining.ShouldEqual([1]);
}
