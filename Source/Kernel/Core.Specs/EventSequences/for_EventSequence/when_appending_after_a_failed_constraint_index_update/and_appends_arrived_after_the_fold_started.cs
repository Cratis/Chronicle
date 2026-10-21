// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_after_a_failed_constraint_index_update;

public class and_appends_arrived_after_the_fold_started : given.an_unfinished_constraint_repair
{
    AppendResult _atFixedEnd;
    bool _claimAtFixedEnd;
    bool _releasedAfterNextFold;

    async Task Establish()
    {
        var original = _eventType;
        _eventType = new("Removed", EventTypeGeneration.First);
        await AppendAnEvent();
        _eventType = original;
        _eventSourceId = EventSourceId.New();
    }

    async Task Because()
    {
        _clock.Advance(TimeSpan.FromSeconds(5));
        _atFixedEnd = await AppendAnEvent();
        _claimAtFixedEnd = (await _claims.GetFor("some-event-source", _uniqueDefinition)).Any();
        _clock.Advance(TimeSpan.FromSeconds(5));
        _result = await AppendAnEvent();
        _releasedAfterNextFold = !(await _claims.GetFor("some-event-source", _uniqueDefinition)).Any();
    }

    [Fact] void should_keep_the_fixed_end_point_pending_for_later_events() => _atFixedEnd.Errors.ShouldContain(AppendError.ConstraintIndexUnavailable);
    [Fact] void should_first_write_the_fixed_end_points_claim() => _claimAtFixedEnd.ShouldBeTrue();
    [Fact] void should_fold_the_later_removal_next() => _releasedAfterNextFold.ShouldBeTrue();
    [Fact] void should_admit_after_both_ranges_are_repaired() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_fold_the_two_ranges_separately() => _rangeReads.ShouldEqual(3);
}
