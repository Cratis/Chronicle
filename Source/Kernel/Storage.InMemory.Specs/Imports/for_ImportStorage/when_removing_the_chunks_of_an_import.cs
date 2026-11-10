// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Imports;
using Cratis.Chronicle.Storage.Imports;

namespace Cratis.Chronicle.Storage.InMemory.Imports.for_ImportStorage;

public class when_removing_the_chunks_of_an_import : given.an_import_storage
{
    async Task Because()
    {
        await _storage.Save(new ImportRecord(_importId, "log", OnInvalidEvent.Reject, DateTimeOffset.UnixEpoch));
        await _storage.AddChunk(Chunk(_importId, 0, 2));
        await _storage.RemoveChunks(_importId);
    }

    [Fact] async Task should_keep_the_import() => (await FreshStorage().Get(_importId)).ShouldNotBeNull();
    [Fact] async Task should_remove_the_chunks() => (await FreshStorage().GetChunk(_importId, 0)).ShouldBeNull();
}
