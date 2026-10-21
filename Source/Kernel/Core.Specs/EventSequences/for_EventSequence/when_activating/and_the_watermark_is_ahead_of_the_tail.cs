// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_activating;

public class and_the_watermark_is_ahead_of_the_tail : given.an_event_sequence_with_repairable_indexes
{
    protected override EventSequenceNumber InitialNext => 2;
    protected override EventSequenceNumber? Watermark => 5;
    EventSequenceNumber? _watermark;

    void Because() => _watermark = _stateStorage.State.ConstraintIndexWatermark;

    [Fact] void should_clamp_coverage_to_the_rebuilt_tail() => _watermark.ShouldEqual((EventSequenceNumber)2);
}
