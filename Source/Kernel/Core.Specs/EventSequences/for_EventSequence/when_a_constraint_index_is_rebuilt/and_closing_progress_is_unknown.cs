// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_a_constraint_index_is_rebuilt;

public class and_closing_progress_is_unknown : given.a_catching_up_sequence
{
    readonly ConstraintIndexRebuildRunId _run = new(Guid.NewGuid(), Guid.NewGuid());
    protected override bool Closing => true;
    protected override EventSequenceNumber? Watermark => 0;
    protected override EventSequenceNumber InitialNext => 0;

    async Task Establish()
    {
        await _eventSequence.ConstraintIndexRebuildStarting(_run, [_closingDefinition.Name]);
        await AppendAnEvent();

        // An empty-cursor rebuild can reopen a snapshot scope after a live close.
        await _closures.Reopen(_closingDefinition.Name.Value, new(EventSourceId: _eventSourceId));
    }

    async Task Because()
    {
        await _eventSequence.ConstraintIndexRebuilt(_run, [_closingDefinition.Name], ConstraintIndexRebuildOutcome.ProgressUnknown);
        await Tick();
    }

    [Fact] async Task should_restore_the_live_close_from_the_start_of_the_run() => (await _closures.GetFor(_closingDefinition.Name.Value, new(EventSourceId: _eventSourceId))).ShouldNotBeNull();
    [Fact] void should_persist_the_completed_watermark() => _snapshots.Written!.ConstraintIndexWatermark.ShouldEqual((EventSequenceNumber)1);
}
