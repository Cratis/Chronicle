// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_repairing_constraint_indexes_on_the_timer;

public class and_the_live_position_lags : given.an_event_sequence_with_repairable_indexes
{
    protected override EventSequenceNumber InitialNext => 1;
    protected override EventSequenceNumber? Watermark => 1;
    EventSequenceNumber? _watermarkBeforeRepair;

    async Task Establish()
    {
        await Seed(0);
        _failWrites = true;
        await AppendAnEvent();
        _failWrites = false;
        await Seed(2, type: new("OtherEvent", EventTypeGeneration.First));
        _stateStorage.State.SequenceNumber = 3;
        _watermarkBeforeRepair = (await _log.GetState()).ConstraintIndexWatermark;
    }

    Task Because() => given.ConstraintRepairTimer.Tick(_silo);

    [Fact] void should_start_with_only_the_failed_live_update_unconfirmed() => _watermarkBeforeRepair.ShouldEqual((EventSequenceNumber)1);
    [Fact] async Task should_fold_the_gap_for_the_other_constraint() => (await _claims.GetFor(_eventSourceId, _otherDefinition)).ShouldNotBeEmpty();
    [Fact] async Task should_persist_only_after_folding_the_gap() => (await _log.GetState()).ConstraintIndexWatermark.ShouldEqual((EventSequenceNumber)3);
}
