// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.EventSequences.Mutations.for_EventSequenceMutationOrdinal;

public class when_checking_reserved_values_and_backing_type : Specification
{
    [Fact] void should_reserve_zero_for_not_set() => EventSequenceMutationOrdinal.NotSet.Value.ShouldEqual(0L);
    [Fact] void should_start_assigning_at_one() => EventSequenceMutationOrdinal.First.Value.ShouldEqual(1L);
    [Fact] void should_be_backed_by_a_long() => typeof(EventSequenceMutationOrdinal).GetProperty(nameof(EventSequenceMutationOrdinal.Value))!.PropertyType.ShouldEqual(typeof(long));
}
