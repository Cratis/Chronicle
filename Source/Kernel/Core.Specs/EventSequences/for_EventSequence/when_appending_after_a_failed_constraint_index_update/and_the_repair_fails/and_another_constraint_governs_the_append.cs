// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_after_a_failed_constraint_index_update.and_the_repair_fails;

public class and_another_constraint_governs_the_append : given.an_event_sequence_with_repairable_indexes
{
    async Task Establish()
    {
        _failWrites = true;
        await AppendAnEvent();
        _eventType = new("OtherEvent", EventTypeGeneration.First);
    }
    async Task Because() => _result = await AppendAnEvent();

    [Fact] void should_admit_the_independent_append() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_not_attempt_to_repair_an_unrelated_constraint() => _rangeReads.ShouldEqual(0);
    [Fact] void should_keep_the_failed_constraint_pending() => _stateStorage.State.ConstraintIndexWatermark.ShouldEqual(EventSequenceNumber.First);
    [Fact] void should_store_both_events() => _log.Events.Count.ShouldEqual(2);
}
