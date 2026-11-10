// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Imports;
using Cratis.Chronicle.Storage.Imports;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Imports.for_ImportStorage;

public class when_removing_an_import : given.an_import_storage
{
    async Task Because()
    {
        await _storage.Save(new ImportRecord(_importId, "log", OnInvalidEvent.Reject, DateTimeOffset.UnixEpoch));
        await _storage.Save(new ImportRecord(_otherImportId, "log", OnInvalidEvent.Reject, DateTimeOffset.UnixEpoch));
        await _storage.AddChunk(Chunk(_importId, 0, 2));
        await _storage.AddChunk(Chunk(_otherImportId, 0, 2));
        await _storage.Remove(_importId);
    }

    [Fact] async Task should_remove_the_import() => (await FreshStorage().Get(_importId)).ShouldBeNull();
    [Fact] async Task should_remove_its_chunks() => (await FreshStorage().GetChunkSummaries(_importId)).ShouldBeEmpty();
    [Fact] async Task should_keep_other_imports() => (await FreshStorage().Get(_otherImportId)).ShouldNotBeNull();
    [Fact] async Task should_keep_the_chunks_of_other_imports() => (await FreshStorage().GetChunkSummaries(_otherImportId)).Count().ShouldEqual(1);
}
