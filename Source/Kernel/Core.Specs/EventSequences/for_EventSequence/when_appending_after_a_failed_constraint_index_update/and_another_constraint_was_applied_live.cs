// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_after_a_failed_constraint_index_update;

public class and_another_constraint_was_applied_live : given.an_event_sequence_with_repairable_indexes
{
    int _otherWrites;

    async Task Establish()
    {
        _failWrites = true;
        await AppendAnEvent();
        _failWrites = false;
        _claimWrites = 0;
        var original = _eventType;
        _eventType = new("OtherEvent", EventTypeGeneration.First);
        await AppendAnEvent();
        _otherWrites = _claimWrites;
        _eventType = original;
    }

    async Task Because()
    {
        _eventSourceId = EventSourceId.New();
        _result = await AppendAnEvent();
    }

    [Fact] void should_write_only_the_pending_constraints_difference() => _claimWrites.ShouldEqual(_otherWrites + 1);
    [Fact] void should_keep_the_live_other_claim() => _otherWrites.ShouldEqual(1);
    [Fact] void should_validate_after_the_pending_range_is_complete() => _result.ConstraintViolations.ShouldNotBeEmpty();
}
