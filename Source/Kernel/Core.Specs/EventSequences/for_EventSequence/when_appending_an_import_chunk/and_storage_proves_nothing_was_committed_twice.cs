// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_an_import_chunk;

public class and_storage_proves_nothing_was_committed_twice : given.an_event_sequence_with_imports
{
    void Establish() => RefuseWrites();

    async Task Because() => _chunkResult = await AppendChunk();

    [Fact] void should_not_settle_so_the_chunk_is_tried_again() => _chunkResult.IsSettled.ShouldBeFalse();
    [Fact] void should_report_why() => _chunkResult.Errors.ShouldNotBeEmpty();
}
