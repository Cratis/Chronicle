// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Imports;

namespace Cratis.Chronicle.Storage.Imports.for_StagedImportChunk.when_comparing_with_the_chunk_staged_under_its_index;

public class and_it_was_removed_before_it_could_be_read : Specification
{
    readonly StagedImportChunk _chunk = new(ImportId.New(), 2, 20, 3, [], []);
    StagedChunkAddition _result;

    void Because() => _result = _chunk.ComparedWith(null);

    [Fact] void should_not_report_it_as_staged() => _result.ShouldEqual(StagedChunkAddition.Conflicting);
}
