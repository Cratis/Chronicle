// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_repairing_constraint_indexes_on_the_timer;

public class and_an_event_is_poisoned : given.an_event_sequence_with_repairable_indexes
{
    readonly given.RepairClock _clock = new();
    EventSequenceNumber? _beforeFifth;
    EventSequenceNumber? _persistedAfterRepair;
    EventSourceId _poisonedSource;
    protected override EventSequenceNumber InitialNext => 1;

    async Task Establish()
    {
        await Seed(0);
        _poisonedSource = _eventSourceId;
        _eventSequence.ConstraintIndexTimeProvider = _clock;
        _eventTypesStorage.GetFor(_eventType.Id, _eventType.Generation).Returns(_ => Task.FromException<EventTypeSchema>(new FormatException("bad schema")));
        for (var attempt = 0; attempt < 4; attempt++)
        {
            await given.ConstraintRepairTimer.Tick(_silo);
            _clock.Advance(TimeSpan.FromSeconds(60));
        }
        _beforeFifth = _stateStorage.State.ConstraintIndexWatermark;
    }

    async Task Because()
    {
        await given.ConstraintRepairTimer.Tick(_silo);
        _clock.Advance(TimeSpan.FromSeconds(60));
        await given.ConstraintRepairTimer.Tick(_silo);
        _persistedAfterRepair = (await _log.GetState()).ConstraintIndexWatermark;
        _eventType = new(_eventType.Id, new EventTypeGeneration(2));
        _eventTypesStorage.GetFor(_eventType.Id, _eventType.Generation).Returns(new EventTypeSchema(_eventType, EventTypeOwner.Server, EventTypeSource.Code, new JsonSchema()));
        _eventSourceId = EventSourceId.New();
        _result = await AppendAnEvent();
    }

    [Fact] void should_not_settle_before_five_identical_failures() => _beforeFifth.ShouldEqual(EventSequenceNumber.First);
    [Fact] void should_advance_past_the_poison_event() => _persistedAfterRepair.ShouldEqual((EventSequenceNumber)1);
    [Fact] void should_admit_a_new_governed_append_after_settlement() => _result.IsSuccess.ShouldBeTrue();
    [Fact] async Task should_not_invent_the_missing_effect() => (await _claims.GetFor(_poisonedSource, _uniqueDefinition)).ShouldBeEmpty();
}
