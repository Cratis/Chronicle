// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Imports;

namespace Cratis.Chronicle.Imports.for_Import.when_a_chunk_is_committed;

/// <summary>
/// The import was failed while the commit was appending its second chunk. The chunk is in the log all the same.
/// </summary>
public class and_the_import_was_failed_meanwhile : given.an_import
{
    async Task Establish()
    {
        await Begin();
        await StageChunk(0, 2);
        await StageChunk(1, 2);
        await _grain.Complete(2, 4);
        await _grain.CommitStarted(new EventSequenceNumber(20));
        await _grain.ChunkCommitted(0, [new(0, "event-type+1", ImportEventOutcomeKind.Appended, 20, []), new(1, "event-type+1", ImportEventOutcomeKind.Appended, 21, [])]);
        await _grain.Fail("An operator removed the commit job.");
    }

    async Task Because() => await _grain.ChunkCommitted(1,
    [
        new(10, "event-type+1", ImportEventOutcomeKind.Appended, 24, []),
        new(11, "event-type+1", ImportEventOutcomeKind.Rejected, EventSequenceNumber.Unavailable, ["unique: taken"])
    ]);

    [Fact] void should_stay_failed() => Record.State.ShouldEqual(ImportState.Failed);
    [Fact] void should_keep_why_it_failed() => Record.FailureReason.ShouldEqual("An operator removed the commit job.");
    [Fact] void should_count_the_events_in_the_log() => Record.EventsCommitted.ShouldEqual(3L);
    [Fact] void should_record_the_range_of_the_chunk() => Record.LastSequenceNumber.ShouldEqual(24UL);
    [Fact] void should_checkpoint_the_chunk() => Record.LastCommittedChunk.ShouldEqual(1);
    [Fact] void should_report_its_rejection() => Record.Rejections.Single().Index.ShouldEqual(11L);
}
