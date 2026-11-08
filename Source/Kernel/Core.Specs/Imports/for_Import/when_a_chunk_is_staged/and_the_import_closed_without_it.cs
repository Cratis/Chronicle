// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Imports;

namespace Cratis.Chronicle.Imports.for_Import.when_a_chunk_is_staged;

public class and_the_import_closed_without_it : given.an_import
{
    bool _kept;

    async Task Establish()
    {
        await Begin();
        await StageChunk(0, 3);
        await _grain.Complete(1, 3);
    }

    async Task Because() => _kept = await _grain.ChunkStaged(new ImportChunkSummary(1, 3, 2, 2, 0, ["event-type"]));

    [Fact] void should_tell_the_stager_to_remove_it() => _kept.ShouldBeFalse();
}
