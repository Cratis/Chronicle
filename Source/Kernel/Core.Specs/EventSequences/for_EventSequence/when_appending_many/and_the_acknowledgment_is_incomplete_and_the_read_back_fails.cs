// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_many;

public class and_the_acknowledgment_is_incomplete_and_the_read_back_fails : given.an_append_with_settlement
{
    void Establish()
    {
        _incomplete = true;
        _probeFails = true;
    }
    async Task Because() => await AppendBatch();
    [Fact] void should_not_throw() => _error.ShouldBeNull();
    [Fact] void should_report_unknown() => _batchResult.Errors.ShouldContain((AppendError)"AppendOutcomeUnknown");
    [Fact] void should_leave_every_constraint_pending() => _stateStorage.State.ConstraintIndexWatermark.ShouldEqual(Concepts.Events.EventSequenceNumber.First);
    [Fact] async Task should_spill_delivery_to_catch_up() => await _appendedEventsQueues.Received(1).SpillToCatchup();
}
