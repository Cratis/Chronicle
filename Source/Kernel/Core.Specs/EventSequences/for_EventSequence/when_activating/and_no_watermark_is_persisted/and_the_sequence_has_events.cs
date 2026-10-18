// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_activating.and_no_watermark_is_persisted;

public class and_the_sequence_has_events : given.an_event_sequence_with_repairable_indexes
{
    protected override EventSequenceNumber? Watermark => null;
    protected override EventSequenceNumber InitialNext => 1001;
    EventSequenceNumber? _boundaryAfterFirstTurn;
    int _writesAfterFirstTurn;
    EventSequenceNumber? _boundaryAfterCompletion;

    async Task Establish()
    {
        for (var number = 0; number < 1001; number++) await Seed((ulong)number);
    }

    async Task Because()
    {
        await given.ConstraintRepairTimer.Tick(_silo);
        _boundaryAfterFirstTurn = _stateStorage.State.ConstraintIndexCatchUpBelow;
        _writesAfterFirstTurn = _claimWrites;
        _result = await AppendAnEvent();
        await given.ConstraintRepairTimer.Tick(_silo);
        _boundaryAfterCompletion = _stateStorage.State.ConstraintIndexCatchUpBelow;
    }

    [Fact] void should_keep_the_boundary_until_all_history_is_folded() => _boundaryAfterFirstTurn.ShouldEqual((EventSequenceNumber)1001);
    [Fact] void should_not_write_an_unfinished_fold() => _writesAfterFirstTurn.ShouldEqual(0);
    [Fact] void should_admit_appends_between_chunks() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_clear_the_boundary_after_completion() => _boundaryAfterCompletion.ShouldBeNull();
    [Fact] void should_persist_the_upgrade_boundary_before_the_first_turn() => _snapshots.Snapshots[0].ConstraintIndexCatchUpBelow.ShouldEqual((EventSequenceNumber)1001);
}
