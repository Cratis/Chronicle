// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.InMemory.Events.Constraints.for_UniqueConstraintsStorage.when_saving;

[PerFactSpecificationLifecycle] // One fact saves the released value for another owner while the others inspect its availability.
public class and_the_owner_moves_to_a_free_value : given.a_claimed_value
{
    async Task Because() => await _storage.Save(_owner, _definition, 43UL, "new-value");

    [Fact] async Task should_release_the_previous_value() => (await _storage.IsAllowed(_otherOwner, _definition, _value)).IsAllowed.ShouldBeTrue();
    [Fact] async Task should_reserve_the_new_value() => (await _storage.IsAllowed(_otherOwner, _definition, "new-value")).IsAllowed.ShouldBeFalse();
    [Fact] async Task should_record_the_new_claim_position() => (await _storage.IsAllowed(_otherOwner, _definition, "new-value")).SequenceNumber.ShouldEqual((EventSequenceNumber)43UL);
    [Fact] async Task should_allow_another_owner_to_save_the_released_value() => (await Catch.Exception(() => _storage.Save(_otherOwner, _definition, 44UL, _value))).ShouldBeNull();
}
