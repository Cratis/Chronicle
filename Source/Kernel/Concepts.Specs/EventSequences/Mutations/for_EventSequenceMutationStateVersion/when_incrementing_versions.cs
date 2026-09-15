// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.EventSequences.Mutations.for_EventSequenceMutationStateVersion;

public class when_incrementing_versions : Specification
{
    EventSequenceMutationStateVersion _next;

    void Because() => _next = EventSequenceMutationStateVersion.First.Next();

    [Fact] void should_reserve_zero_for_not_set() => EventSequenceMutationStateVersion.NotSet.Value.ShouldEqual(0L);
    [Fact] void should_start_assigning_at_one() => EventSequenceMutationStateVersion.First.Value.ShouldEqual(1L);
    [Fact] void should_increment_with_checked_long_semantics() => _next.Value.ShouldEqual(2L);
}
