// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_an_import_chunk;

public class and_storage_proves_nothing_keyed_was_committed_twice : given.an_event_sequence_with_imports
{
    void Establish() => RefuseWrites();

    async Task Because() => _chunkResult = await AppendChunk([new IdempotencyKey("a"), new IdempotencyKey("b"), new IdempotencyKey("c")]);

    [Fact] void should_not_settle_so_the_chunk_is_tried_again() => _chunkResult.IsSettled.ShouldBeFalse();
    [Fact] void should_not_report_any_event_as_rejected() => _chunkResult.Outcomes.ShouldBeEmpty();
}
