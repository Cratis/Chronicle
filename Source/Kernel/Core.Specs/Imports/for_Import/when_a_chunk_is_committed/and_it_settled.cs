// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Imports.for_Import.when_a_chunk_is_committed;

public class and_it_settled : given.an_import
{
    async Task Establish()
    {
        await Begin();
        await StageChunk(0, 4);
        await _grain.Complete(1, 4);
        await _grain.CommitStarted(new EventSequenceNumber(20));
    }

    async Task Because() => await _grain.ChunkCommitted(0,
    [
        new(0, "event-type+1", ImportEventOutcomeKind.Appended, 20, []),
        new(1, "event-type+1", ImportEventOutcomeKind.AlreadyAppended, 3, []),
        new(2, "event-type+1", ImportEventOutcomeKind.Rejected, EventSequenceNumber.Unavailable, ["unique: taken"]),
        new(3, "event-type+1", ImportEventOutcomeKind.Appended, 22, [])
    ]);

    [Fact] void should_checkpoint_the_chunk() => Record.LastCommittedChunk.ShouldEqual(0);
    [Fact] void should_count_the_committed_events() => Record.EventsCommitted.ShouldEqual(2L);
    [Fact] void should_count_the_already_appended_events() => Record.AlreadyAppended.ShouldEqual(1L);
    [Fact] void should_report_the_rejection_by_its_index() => Record.Rejections.Single().Index.ShouldEqual(2L);
    [Fact] void should_record_the_range_it_was_committed_at() => (Record.FirstSequenceNumber, Record.LastSequenceNumber).ShouldEqual((20UL, 22UL));
    [Fact] void should_continue_after_the_range() => Record.CommittedFrom.ShouldEqual(23UL);
}
