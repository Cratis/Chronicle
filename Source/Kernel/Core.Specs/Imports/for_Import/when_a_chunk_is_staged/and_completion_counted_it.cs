// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Imports;

namespace Cratis.Chronicle.Imports.for_Import.when_a_chunk_is_staged;

public class and_completion_counted_it : given.an_import
{
    bool _kept;

    async Task Establish()
    {
        await Begin();
        await StageChunk(0, 3);
        await _grain.Complete(1, 3);
    }

    async Task Because() => _kept = await _grain.ChunkStaged(new ImportChunkSummary(0, 0, 3, 3, 0, ["event-type"]));

    [Fact] void should_keep_it() => _kept.ShouldBeTrue();
}
