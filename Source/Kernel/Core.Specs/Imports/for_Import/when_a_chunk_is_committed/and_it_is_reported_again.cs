// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Imports.for_Import.when_a_chunk_is_committed;

public class and_it_is_reported_again : given.an_import
{
    async Task Establish()
    {
        await Begin();
        await StageChunk(0, 1);
        await _grain.Complete(1, 1);
        await _grain.CommitStarted(new EventSequenceNumber(20));
        await _grain.ChunkCommitted(0, [new(0, "event-type+1", ImportEventOutcomeKind.Appended, 20, [])]);
    }

    async Task Because() => await _grain.ChunkCommitted(0, [new(0, "event-type+1", ImportEventOutcomeKind.Appended, 20, [])]);

    [Fact] void should_count_it_once() => Record.EventsCommitted.ShouldEqual(1L);
}
