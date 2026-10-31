// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Events.Constraints;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_catching_up_constraint_indexes;

public class and_another_constraint_keeps_failing_repair : given.a_catching_up_sequence_with_a_failing_constraint
{
    protected override EventSequenceNumber InitialNext => 5;
    int _savesAfterFirstRepair;
    int _savesAfterIdleTick;
    int _savesAfterSecondRepair;
    UniqueConstraintValue[] _newHolder;
    UniqueConstraintValue[] _oldHolder;
    EventSequenceNumber? _boundaryAfterCatchUp;
    int _excludedAfterCatchUp;
    bool _admittedAfterCatchUp;

    async Task Establish()
    {
        await TransferHistory();
        await Seed(3, "other-holder", new("OtherEvent", EventTypeGeneration.First), "other-value");
        await Seed(4, "other-mover", new("OtherEvent", EventTypeGeneration.First), "old-other");
        OccupySlots();
        _otherFails = true;
        await AppendOther("other-mover");
    }

    async Task Because()
    {
        await Tick();
        _savesAfterFirstRepair = _otherSaves;
        await Tick();
        _newHolder = await Values("new-holder");
        _oldHolder = await Values("old-holder");
        _boundaryAfterCatchUp = _stateStorage.State.ConstraintIndexCatchUpBelow;
        _excludedAfterCatchUp = _eventSequence.ConstraintIndexCatchUpExcludedKeyCount;
        _admittedAfterCatchUp = WaitingAdmitted();
        ConstraintIndexCatchUpSlots.For(_storage).Complete(_waiting);
        await Tick();
        _savesAfterIdleTick = _otherSaves;
        _clock.Advance(TimeSpan.FromSeconds(5));
        await Tick();
        _savesAfterSecondRepair = _otherSaves;
        _otherFails = false;
        _clock.Advance(TimeSpan.FromMinutes(2));
        for (var tick = 0; tick < 6 && _stateStorage.State.ConstraintIndexCatchUpBelow is not null; tick++) await Tick();
    }

    [Fact] void should_catch_up_the_healthy_constraint_while_the_other_fails_repair() => _newHolder.ShouldContainOnly(Hash("value"));
    [Fact] void should_release_the_superseded_historical_claim() => _oldHolder.ShouldBeEmpty();
    [Fact] void should_keep_the_boundary_for_the_constraint_pending_repair() => _boundaryAfterCatchUp.ShouldEqual((EventSequenceNumber)5);
    [Fact] void should_discard_catch_up_memory_when_nothing_is_eligible() => _excludedAfterCatchUp.ShouldEqual(0);
    [Fact] void should_admit_a_waiting_sequence() => _admittedAfterCatchUp.ShouldBeTrue();
    [Fact] void should_not_reset_the_repair_back_off_from_catch_up() => _savesAfterIdleTick.ShouldEqual(_savesAfterFirstRepair);
    [Fact] void should_keep_retrying_repair_on_its_own_back_off() => _savesAfterSecondRepair.ShouldBeGreaterThan(_savesAfterIdleTick);
    [Fact] async Task should_catch_up_the_recovered_constraint_after_readmission() => (await OtherValues("other-holder")).ShouldContainOnly(Hash("other-value"));
    [Fact] async Task should_not_overwrite_a_live_key_after_rebuilding_exclusions() => (await OtherValues("other-mover")).ShouldContainOnly(Hash("claimed@example.com"));
    [Fact] void should_finish_the_catch_up() => _stateStorage.State.ConstraintIndexCatchUpBelow.ShouldBeNull();
}
