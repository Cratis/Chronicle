// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_repairing_constraint_indexes_on_the_timer;

public class and_an_event_failure_is_transient : given.an_event_sequence_with_repairable_indexes
{
    readonly given.RepairClock _clock = new();
    protected override EventSequenceNumber InitialNext => 1;

    async Task Establish()
    {
        await Seed(0);
        _eventSequence.ConstraintIndexTimeProvider = _clock;
        _eventTypesStorage.GetFor(_eventType.Id, _eventType.Generation).Returns(_ => Task.FromException<Concepts.EventTypes.EventTypeSchema>(new TimeoutException()));
    }

    async Task Because()
    {
        for (var attempt = 0; attempt < 6; attempt++)
        {
            await given.ConstraintRepairTimer.Tick(_silo);
            _clock.Advance(TimeSpan.FromSeconds(60));
        }
    }

    [Fact] void should_not_settle_a_transient_failure() => _stateStorage.State.ConstraintIndexWatermark.ShouldEqual(EventSequenceNumber.First);
    [Fact] void should_continue_attempting_repairs() => _rangeReads.ShouldEqual(6);
}
