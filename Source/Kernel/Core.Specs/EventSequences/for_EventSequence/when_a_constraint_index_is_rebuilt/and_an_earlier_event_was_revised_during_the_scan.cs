// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_a_constraint_index_is_rebuilt;

public class and_an_earlier_event_was_revised_during_the_scan : when_revising.a_revisable_sequence
{
    readonly ConstraintIndexRebuildRunId _runId = new(Guid.NewGuid());
    protected override EventSequenceNumber InitialNext => 3;
    protected override EventSequenceNumber? Watermark => 3;

    async Task Establish()
    {
        await Seed(1, "another-source");
        await Seed(2, "last-source");
        await _eventSequence.ConstraintIndexRebuildStarting(_runId, [_uniqueDefinition.Name]);
        await ReviseFirstEvent();
        _failReads = true;
    }

    Task Because() => _eventSequence.ConstraintIndexRebuilt(_runId, [_uniqueDefinition.Name], new(true, 2));

    [Fact] void should_have_revised_the_event() => _error.ShouldBeNull();
    [Fact] void should_keep_the_revision_pending_below_the_processed_position() => _stateStorage.State.ConstraintIndexWatermark.ShouldEqual(EventSequenceNumber.First);
    [Fact] void should_persist_the_revision_boundary_across_handover() => _snapshots.Written!.ConstraintIndexWatermark.ShouldEqual(EventSequenceNumber.First);
    [Fact] void should_attempt_to_refold_the_revised_history() => _rangeReads.ShouldBeGreaterThan(0);
}
