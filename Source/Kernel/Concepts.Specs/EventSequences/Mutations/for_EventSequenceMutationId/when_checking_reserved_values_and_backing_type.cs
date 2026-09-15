// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.EventSequences.Mutations.for_EventSequenceMutationId;

public class when_checking_reserved_values_and_backing_type : Specification
{
    [Fact] void should_reserve_the_empty_guid_for_not_set() => EventSequenceMutationId.NotSet.Value.ShouldEqual(Guid.Empty);
    [Fact] void should_be_backed_by_a_guid() => typeof(EventSequenceMutationId).GetProperty(nameof(EventSequenceMutationId.Value))!.PropertyType.ShouldEqual(typeof(Guid));
}
