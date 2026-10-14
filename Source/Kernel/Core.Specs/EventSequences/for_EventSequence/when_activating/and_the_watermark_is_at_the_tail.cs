// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_activating;

public class and_the_watermark_is_at_the_tail : given.an_event_sequence_with_repairable_indexes
{
    protected override EventSequenceNumber InitialNext => 2;
    protected override EventSequenceNumber? Watermark => 2;
    async Task Because() => _result = await AppendAnEvent();

    [Fact] void should_append_without_replay() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_not_read_history() => _rangeReads.ShouldEqual(0);
    [Fact] void should_advance_the_watermark() => _stateStorage.State.ConstraintIndexWatermark.ShouldEqual((EventSequenceNumber)3);
}
