// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Imports;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Imports.for_ImportStorage;

public class when_adding_a_chunk_twice : given.an_import_storage
{
    StagedChunkAddition _first;
    StagedChunkAddition _second;
    StagedImportChunk? _stored;

    async Task Because()
    {
        _first = await _storage.AddChunk(Chunk(_importId, 0, 2));
        _second = await _storage.AddChunk(Chunk(_importId, 0, 2));
        _stored = await FreshStorage().GetChunk(_importId, 0);
    }

    [Fact] void should_store_the_first() => _first.ShouldEqual(StagedChunkAddition.Added);
    [Fact] void should_report_the_second_as_already_staged() => _second.ShouldEqual(StagedChunkAddition.AlreadyStaged);
    [Fact] void should_keep_the_first() => _stored!.EventsReceived.ShouldEqual(2);
}
