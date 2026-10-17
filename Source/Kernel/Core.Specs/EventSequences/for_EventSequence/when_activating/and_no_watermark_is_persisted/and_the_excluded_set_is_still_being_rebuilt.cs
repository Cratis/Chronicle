// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_activating.and_no_watermark_is_persisted;

public class and_the_excluded_set_is_still_being_rebuilt : given.a_catching_up_sequence
{
    EventSequenceNumber? _boundaryAfterFirstTurn;
    int _writesAfterFirstTurn;

    async Task Establish()
    {
        for (ulong number = 0; number < 1004; number++) await Seed(number);
        _stateStorage.State.SequenceNumber = 1004;
        _stateStorage.State.ConstraintIndexWatermark = 1004;
        await _log.SaveState(_stateStorage.State);
        await Reactivate();
    }

    async Task Because()
    {
        await Tick();
        _boundaryAfterFirstTurn = _stateStorage.State.ConstraintIndexCatchUpBelow;
        _writesAfterFirstTurn = _claimWrites;
        await Tick();
    }

    [Fact] void should_keep_the_boundary_until_exclusions_are_complete() => _boundaryAfterFirstTurn.ShouldEqual((EventSequenceNumber)3);
    [Fact] void should_not_write_during_the_rebuild() => _writesAfterFirstTurn.ShouldEqual(0);
    [Fact] void should_not_write_a_live_key_after_the_rebuild() => _claimWrites.ShouldEqual(0);
    [Fact] void should_complete_after_exclusions_and_folding() => _stateStorage.State.ConstraintIndexCatchUpBelow.ShouldBeNull();
}
