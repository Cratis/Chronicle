// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.InMemory.Events.Constraints.for_UniqueConstraintsStorage.when_saving;

public class and_the_owner_reclaims_the_same_value : given.a_claimed_value
{
    async Task Because() => await _storage.Save(_owner, _definition, 43UL, _value);

    [Fact] async Task should_allow_the_owner() => (await _storage.IsAllowed(_owner, _definition, _value)).IsAllowed.ShouldBeTrue();
    [Fact] async Task should_refuse_another_owner() => (await _storage.IsAllowed(_otherOwner, _definition, _value)).IsAllowed.ShouldBeFalse();
    [Fact] async Task should_preserve_the_original_claim_position() => (await _storage.IsAllowed(_otherOwner, _definition, _value)).SequenceNumber.ShouldEqual(_sequenceNumber);
}
