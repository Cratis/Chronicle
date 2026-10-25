// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_revising;

public class and_the_type_is_not_constrained : a_revisable_sequence
{
    readonly EventType _unconstrained = new("unconstrained-event", EventTypeGeneration.First);

    protected override EventSequenceNumber InitialNext => 2;
    protected override EventSequenceNumber? Watermark => 2;

    async Task Establish() => await Seed(1, type: _unconstrained);

    async Task Because() => await ReviseAt(1, _unconstrained);

    [Fact] void should_not_fail() => _error.ShouldBeNull();
    [Fact] void should_not_write_state() => _snapshots.Snapshots.Count.ShouldEqual(_stateWritesBeforeRevise);
    [Fact] void should_not_lower_the_watermark() => _stateStorage.State.ConstraintIndexWatermark.ShouldEqual((EventSequenceNumber)2);
    [Fact] void should_revise_in_storage() => _storageRevised.ShouldBeTrue();
}
