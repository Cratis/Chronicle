// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_activating.and_no_watermark_is_persisted;

public class and_the_sequence_is_empty : given.an_event_sequence_with_repairable_indexes
{
    protected override EventSequenceNumber? Watermark => null;

    Task Because() => given.ConstraintRepairTimer.Tick(_silo);

    [Fact] void should_persist_the_initial_watermark() => _snapshots.Snapshots.Single().ConstraintIndexWatermark.ShouldEqual(EventSequenceNumber.First);
    [Fact] void should_not_start_a_catch_up() => _stateStorage.State.ConstraintIndexCatchUpBelow.ShouldBeNull();
    [Fact] void should_not_read_history() => _rangeReads.ShouldEqual(0);
}
