// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.EventSequences.Mutations.for_EventSequenceMutationTarget;

public class when_describing_an_empty_range : Specification
{
    EventSequenceMutationTarget _target;

    void Because() => _target = new(EventSequenceNumber.First, EventSequenceNumber.First, EventCount.Zero);

    [Fact] void should_start_at_zero() => _target.Start.ShouldEqual(EventSequenceNumber.First);
    [Fact] void should_end_exclusively_at_zero() => _target.EndExclusive.ShouldEqual(EventSequenceNumber.First);
    [Fact] void should_expect_no_events() => _target.ExpectedCount.ShouldEqual(EventCount.Zero);
}
