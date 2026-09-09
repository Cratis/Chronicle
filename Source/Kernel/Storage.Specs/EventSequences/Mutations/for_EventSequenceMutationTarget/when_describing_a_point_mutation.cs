// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.EventSequences.Mutations.for_EventSequenceMutationTarget;

public class when_describing_a_point_mutation : Specification
{
    readonly EventSequenceNumber _sequenceNumber = new(42UL);
    EventSequenceMutationTarget _target;

    void Because() => _target = new(_sequenceNumber, _sequenceNumber.Next(), new EventCount(1UL));

    [Fact] void should_start_at_the_point() => _target.Start.ShouldEqual(_sequenceNumber);
    [Fact] void should_end_exclusively_after_the_point() => _target.EndExclusive.ShouldEqual(_sequenceNumber.Next());
    [Fact] void should_expect_one_event() => _target.ExpectedCount.ShouldEqual(new EventCount(1UL));
}
