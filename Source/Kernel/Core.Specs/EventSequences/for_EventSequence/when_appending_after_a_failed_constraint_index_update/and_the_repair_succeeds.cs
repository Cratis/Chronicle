// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_after_a_failed_constraint_index_update;

public class and_the_repair_succeeds : given.an_event_sequence_with_repairable_indexes
{
    async Task Establish()
    {
        _failWrites = true;
        await AppendAnEvent();
        _failWrites = false;
        _eventSourceId = EventSourceId.New();
    }
    async Task Because() => _result = await AppendAnEvent();

    [Fact] void should_refuse_the_duplicate_after_restoring_the_claim() => _result.ConstraintViolations.ShouldNotBeEmpty();
    [Fact] void should_store_only_the_original_event() => _log.Events.Count.ShouldEqual(1);
    [Fact] void should_read_the_pending_range_once() => _rangeReads.ShouldEqual(1);
    [Fact] void should_advance_the_watermark_after_repair() => _stateStorage.State.ConstraintIndexWatermark.ShouldEqual((EventSequenceNumber)1);
}
