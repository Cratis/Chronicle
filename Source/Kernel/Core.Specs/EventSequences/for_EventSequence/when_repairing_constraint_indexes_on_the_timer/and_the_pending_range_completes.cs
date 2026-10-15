// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_repairing_constraint_indexes_on_the_timer;

public class and_the_pending_range_completes : given.an_event_sequence_with_repairable_indexes
{
    int _before;
    protected override EventSequenceNumber InitialNext => 1;

    async Task Establish()
    {
        await Seed(0);
        _before = _silo.TimerRegistry.NumberOfActiveTimers;
    }

    Task Because() => given.ConstraintRepairTimer.Tick(_silo);

    [Fact] void should_register_repair_separately_from_pattern_capture() => _before.ShouldEqual(2);
    [Fact] void should_release_the_repair_keep_alive_on_completion() => _silo.TimerRegistry.NumberOfActiveTimers.ShouldEqual(1);
}
