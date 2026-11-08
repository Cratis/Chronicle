// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Imports;
using Cratis.Chronicle.Storage.Imports;

namespace Cratis.Chronicle.Imports.for_ImportStager.when_staging;

public class and_the_import_closes_while_the_chunk_is_written : given.a_stager
{
    void Establish()
    {
        _import.ChunkStaged(Arg.Any<ImportChunkSummary>()).Returns(false);
        _import.Get().Returns(new ImportRecord(_key.ImportId, EventSequenceId.Log, OnInvalidEvent.Reject, DateTimeOffset.UtcNow) { State = ImportState.Abandoned });
    }

    async Task Because() => _result = await Stage(0, Event(), Event());

    [Fact] void should_refuse_the_chunk() => _result.Error.ShouldContain("Abandoned");
    [Fact] async Task should_remove_the_chunk_it_wrote() => (await _imports.GetChunk(_key.ImportId, 0)).ShouldBeNull();
}
