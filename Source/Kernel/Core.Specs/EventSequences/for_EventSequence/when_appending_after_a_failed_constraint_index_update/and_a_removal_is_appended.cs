// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_after_a_failed_constraint_index_update;

public class and_a_removal_is_appended : given.an_event_sequence_with_repairable_indexes
{
    async Task Establish()
    {
        _failWrites = true;
        await AppendAnEvent();
        _eventType = new("Removed", EventTypeGeneration.First);
    }
    async Task Because() => _result = await AppendAnEvent();

    [Fact] void should_admit_the_removal() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_not_repair_before_an_ungoverned_append() => _rangeReads.ShouldEqual(0);
    [Fact] void should_keep_the_constraint_pending_for_the_fold() => _stateStorage.State.ConstraintIndexWatermark.ShouldEqual(EventSequenceNumber.First);
    [Fact] void should_store_the_removal() => _log.Events.Count.ShouldEqual(2);
}
