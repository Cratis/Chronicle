// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_activating.and_no_watermark_is_persisted;

public class and_the_empty_sequence_has_no_constraint_indexes : given.an_empty_sequence_with_a_failing_first_snapshot
{
    Task Because() => given.ConstraintRepairTimer.Tick(_silo);

    [Fact] void should_not_require_a_snapshot_to_activate() => _stateWriteAttempts.ShouldEqual(0);
    [Fact] void should_initialize_the_watermark_in_memory() => _stateStorage.State.ConstraintIndexWatermark.ShouldEqual(EventSequenceNumber.First);
    [Fact] void should_not_start_a_catch_up() => _stateStorage.State.ConstraintIndexCatchUpBelow.ShouldBeNull();
}
