// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Imports.for_EventImport.when_adding;

public class and_the_content_exceeds_the_byte_limit : given.an_event_import
{
    void Establish()
    {
        _maxChunkEvents = 100;
        _maxChunkBytes = 250;
    }

    async Task Because()
    {
        await Import.Add(Events(5, contentLength: 100));
        await Import.Complete();
    }

    [Fact] void should_keep_each_chunk_within_the_bytes() => _chunks.OrderBy(_ => _.ChunkIndex).Select(_ => _.Events.Count()).ShouldEqual([2, 2, 1]);
}
