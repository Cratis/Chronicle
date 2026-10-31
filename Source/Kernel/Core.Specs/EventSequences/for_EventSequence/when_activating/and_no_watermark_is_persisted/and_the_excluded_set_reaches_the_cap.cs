// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_activating.and_no_watermark_is_persisted;

public class and_the_excluded_set_reaches_the_cap : given.a_catching_up_sequence
{
    protected override EventSequenceNumber InitialNext => 1001;
    EventSequenceNumber? _raisedBoundary;

    async Task Establish()
    {
        for (ulong number = 0; number < 1001; number++) await Seed(number, email: "historical");
        _eventSequence.ConstraintIndexCatchUpKeyLimit = 1;
        await Tick();
    }

    async Task Because()
    {
        _eventType = new("Removed", EventTypeGeneration.First);
        await AppendAnEvent();
        _raisedBoundary = _snapshots.Written!.ConstraintIndexCatchUpBelow;
        await Tick();
    }

    [Fact] void should_persist_the_raised_boundary() => _raisedBoundary.ShouldEqual((EventSequenceNumber)1002);
    [Fact] void should_not_write_a_difference_taken_at_the_old_boundary() => _claimWrites.ShouldEqual(0);
    [Fact] async Task should_include_the_later_removal_before_writing() => (await Values(_eventSourceId.Value)).ShouldBeEmpty();
    [Fact] void should_complete_the_extended_fold() => _stateStorage.State.ConstraintIndexCatchUpBelow.ShouldBeNull();
}
