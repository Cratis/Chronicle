// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_many;

public class and_the_acknowledgment_is_incomplete_and_the_read_back_fails : given.an_append_with_settlement
{
    AppendResult _otherResult;

    async Task Establish()
    {
        // Complete a real repair first, then keep its cadence clock fixed. The unknown gap must
        // refuse both constraints while another append-turn repair is correctly throttled.
        _failWrites = true;
        await AppendAnEvent();
        _failWrites = false;
        _eventSequence.ConstraintIndexTimeProvider = new given.RepairClock();
        (await AppendAnEvent()).IsSuccess.ShouldBeTrue();
        _appendedEventsQueues.ClearReceivedCalls();
        _incomplete = true;
        _probeFails = true;
        _failReads = true;

        // Tail visibility can lag the uncertain write. Only the gap mark protects this turn.
        _eventSequenceStorage.GetTailSequenceNumber().Returns((EventSequenceNumber)1UL);
    }

    async Task Because()
    {
        await AppendBatch(governed: true);
        _result = await AppendAnEvent();
        _eventType = new("OtherEvent", EventTypeGeneration.First);
        _otherResult = await AppendAnEvent();
    }

    [Fact] void should_not_throw() => _error.ShouldBeNull();
    [Fact] void should_report_unknown() => _batchResult.Errors.ShouldContain(AppendError.AppendOutcomeUnknown);
    [Fact] void should_leave_every_constraint_pending() => new[] { _result, _otherResult }.ShouldEachConformTo(result => result.Errors.Contains(AppendError.ConstraintIndexUnavailable));
    [Fact] void should_not_write_either_governed_append() => _log.Events.Count.ShouldEqual(4);
    [Fact] async Task should_spill_delivery_to_catch_up() => await _appendedEventsQueues.Received(1).SpillToCatchup();
}
