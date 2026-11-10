// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Imports;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Imports.for_ImportStorage;

public class when_adding_other_events_under_a_staged_index : given.an_import_storage
{
    StagedChunkAddition _second;
    StagedImportChunk? _stored;

    async Task Because()
    {
        await _storage.AddChunk(Chunk(_importId, 0, 2));
        _second = await _storage.AddChunk(Chunk(_importId, 0, 5));
        _stored = await FreshStorage().GetChunk(_importId, 0);
    }

    [Fact] void should_report_a_conflict() => _second.ShouldEqual(StagedChunkAddition.Conflicting);
    [Fact] void should_keep_the_first() => _stored!.EventsReceived.ShouldEqual(2);
}
