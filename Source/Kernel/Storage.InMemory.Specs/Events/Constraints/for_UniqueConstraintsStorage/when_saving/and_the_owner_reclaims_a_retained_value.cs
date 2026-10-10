// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.InMemory.Events.Constraints.for_UniqueConstraintsStorage.when_saving;

public class and_the_owner_reclaims_a_retained_value : given.a_per_value_constraint
{
    async Task Because() => await _storage.Save(_owner, _definition, 46L, "first");

    [Fact] async Task should_keep_the_value_reserved() => (await _storage.IsAllowed(_otherOwner, _definition, "first")).IsAllowed.ShouldBeFalse();
    [Fact] async Task should_allow_the_owner_to_reclaim_it() => (await _storage.IsAllowed(_owner, _definition, "first")).IsAllowed.ShouldBeTrue();
    [Fact] async Task should_preserve_the_original_claim_position() => (await _storage.IsAllowed(_otherOwner, _definition, "first")).SequenceNumber.ShouldEqual((EventSequenceNumber)42L);
}
