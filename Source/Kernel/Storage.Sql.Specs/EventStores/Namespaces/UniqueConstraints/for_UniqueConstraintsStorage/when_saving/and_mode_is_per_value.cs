// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.UniqueConstraints.for_UniqueConstraintsStorage.when_saving;

public class and_mode_is_per_value : given.a_per_value_constraint
{
    async Task Establish() => await _storage.Save(_owner, _definition, 42L, "first");

    async Task Because() => await _storage.Save(_owner, _definition, 43L, "second");

    [Fact] async Task should_keep_the_earlier_value_reserved() => (await _storage.IsAllowed(_otherOwner, _definition, "first")).IsAllowed.ShouldBeFalse();
    [Fact] async Task should_keep_the_new_value_reserved() => (await _storage.IsAllowed(_otherOwner, _definition, "second")).IsAllowed.ShouldBeFalse();
    [Fact] async Task should_allow_the_owner_to_reclaim_the_earlier_value() => (await _storage.IsAllowed(_owner, _definition, "first")).IsAllowed.ShouldBeTrue();
    [Fact] async Task should_preserve_the_original_claim_position() => (await _storage.IsAllowed(_otherOwner, _definition, "first")).SequenceNumber.ShouldEqual((EventSequenceNumber)42L);
}
