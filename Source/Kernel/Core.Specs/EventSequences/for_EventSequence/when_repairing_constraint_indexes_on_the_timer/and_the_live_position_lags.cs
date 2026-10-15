// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_repairing_constraint_indexes_on_the_timer;

public class and_the_live_position_lags : given.an_event_sequence_with_repairable_indexes
{
    protected override EventSequenceNumber InitialNext => 1;

    async Task Establish()
    {
        await Seed(0);
        await Seed(1, type: new("OtherEvent", EventTypeGeneration.First));
        _stateStorage.State.SequenceNumber = 2;
    }

    Task Because() => given.ConstraintRepairTimer.Tick(_silo);

    [Fact] async Task should_fold_the_gap_for_the_other_constraint() => (await _claims.GetFor(_eventSourceId, _otherDefinition)).ShouldNotBeEmpty();
    [Fact] async Task should_persist_only_after_folding_the_gap() => (await _log.GetState()).ConstraintIndexWatermark.ShouldEqual((EventSequenceNumber)2);
}
