// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Events.Constraints;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_catching_up_constraint_indexes;

public class and_the_fold_is_blocked_by_a_constraint_pending_repair : given.a_catching_up_sequence_with_a_failing_constraint
{
    protected override EventSequenceNumber InitialNext => 1002;
    bool _admittedWhileFolding;
    bool _admittedAfterBlock;
    int _excludedBeforeBlock;
    int _excludedAfterBlock;

    async Task Establish()
    {
        for (ulong number = 0; number < 1001; number++) await Seed(number);
        await Seed(1001, "other-holder", new("OtherEvent", EventTypeGeneration.First), "other-value");
        OccupySlots();
    }

    async Task Because()
    {
        await Tick();
        _admittedWhileFolding = WaitingAdmitted();
        _otherFails = true;
        await AppendOther("live-other");
        _excludedBeforeBlock = _eventSequence.ConstraintIndexCatchUpExcludedKeyCount;
        await Tick();
        await Tick();
        _admittedAfterBlock = WaitingAdmitted();
        _excludedAfterBlock = _eventSequence.ConstraintIndexCatchUpExcludedKeyCount;
        ConstraintIndexCatchUpSlots.For(_storage, ConstraintIndexRepairConfiguration.CatchUpSlots).Complete(_waiting);
        for (var tick = 0; tick < 6 && (await Values(_eventSourceId)).Length == 0; tick++) await Tick();
    }

    [Fact] void should_hold_memory_admission_while_folding() => _admittedWhileFolding.ShouldBeFalse();
    [Fact] void should_track_live_keys_while_admitted() => _excludedBeforeBlock.ShouldBeGreaterThan(0);
    [Fact] void should_release_admission_when_the_fold_is_blocked() => _admittedAfterBlock.ShouldBeTrue();
    [Fact] void should_discard_exclusions_when_releasing() => _excludedAfterBlock.ShouldEqual(0);
    [Fact] async Task should_finish_the_healthy_constraint_after_readmission() => (await Values(_eventSourceId)).ShouldContainOnly(Hash("claimed@example.com"));
    [Fact] async Task should_not_write_history_for_the_constraint_pending_repair() => (await OtherValues("other-holder")).ShouldBeEmpty();
    [Fact] void should_keep_the_boundary() => _stateStorage.State.ConstraintIndexCatchUpBelow.ShouldEqual((EventSequenceNumber)1002);
}
