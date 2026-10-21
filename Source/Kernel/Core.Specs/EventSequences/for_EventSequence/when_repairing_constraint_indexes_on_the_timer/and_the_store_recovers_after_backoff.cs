// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_repairing_constraint_indexes_on_the_timer;

public class and_the_store_recovers_after_backoff : given.an_event_sequence_with_repairable_indexes
{
    readonly given.RepairClock _clock = new();
    int _readsBeforeRecovery;
    protected override EventSequenceNumber InitialNext => 1;

    async Task Establish()
    {
        await Seed(0);
        _eventSequence.ConstraintIndexTimeProvider = _clock;
        _failReads = true;
        await given.ConstraintRepairTimer.Tick(_silo);
        _failReads = false;
        await given.ConstraintRepairTimer.Tick(_silo);
        _readsBeforeRecovery = _rangeReads;
        _clock.Advance(TimeSpan.FromSeconds(1));
    }

    Task Because() => given.ConstraintRepairTimer.Tick(_silo);

    [Fact] void should_not_attempt_before_the_backoff() => _readsBeforeRecovery.ShouldEqual(1);
    [Fact] async Task should_heal_without_append_traffic() => (await _claims.GetFor(_eventSourceId, _uniqueDefinition)).ShouldNotBeEmpty();
}
