// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.InMemory.Events.Constraints.for_UniqueConstraintsStorage.when_saving;

public class and_the_value_is_claimed_in_another_scope_or_constraint : given.a_claimed_value
{
    async Task Because()
    {
        await _storage.Save(_otherOwner, _definition, 43UL, _value, "another-scope");
        await _storage.Save(_otherOwner, _definition with { Name = "another-constraint" }, 44UL, _value);
    }

    [Fact] async Task should_preserve_the_original_claim() => (await _storage.IsAllowed(_otherOwner, _definition, _value)).IsAllowed.ShouldBeFalse();
    [Fact] async Task should_allow_the_owner_in_the_other_scope() => (await _storage.IsAllowed(_otherOwner, _definition, _value, "another-scope")).IsAllowed.ShouldBeTrue();
    [Fact] async Task should_reserve_the_value_in_the_other_scope() => (await _storage.IsAllowed(_owner, _definition, _value, "another-scope")).IsAllowed.ShouldBeFalse();
    [Fact] async Task should_allow_the_owner_in_the_other_constraint() => (await _storage.IsAllowed(_otherOwner, _definition with { Name = "another-constraint" }, _value)).IsAllowed.ShouldBeTrue();
    [Fact] async Task should_reserve_the_value_in_the_other_constraint() => (await _storage.IsAllowed(_owner, _definition with { Name = "another-constraint" }, _value)).IsAllowed.ShouldBeFalse();
}
