// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_repairing_constraint_indexes_on_the_timer;

public class and_a_settled_poison_replay_is_discarded : given.an_event_sequence_with_repairable_indexes
{
    readonly given.RepairClock _clock = new();
    int _schemaFailures;
    protected override EventSequenceNumber InitialNext => 1;

    async Task Establish()
    {
        await Seed(0);
        _eventSequence.ConstraintIndexTimeProvider = _clock;
        _eventTypesStorage.GetFor(_eventType.Id, _eventType.Generation).Returns(_ =>
        {
            _schemaFailures++;
            return Task.FromException<EventTypeSchema>(new FormatException("bad schema"));
        });
        for (var attempt = 0; attempt < 5; attempt++)
        {
            await given.ConstraintRepairTimer.Tick(_silo);
            _clock.Advance(TimeSpan.FromSeconds(60));
        }

        // Hard deadlines and definition synchronization both discard the in-memory replay this way.
        typeof(EventSequence).GetField("_constraintIndexReplay", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(_eventSequence, null);
    }

    Task Because() => given.ConstraintRepairTimer.Tick(_silo);

    [Fact] void should_not_repeat_the_poison_failure_run() => _schemaFailures.ShouldEqual(5);
    [Fact] async Task should_persist_the_repaired_watermark_in_the_next_turn() => (await _log.GetState()).ConstraintIndexWatermark.ShouldEqual((EventSequenceNumber)1);
}
