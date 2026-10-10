// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.InMemory.Events.Constraints.for_UniqueConstraintsStorage.when_clearing_retained_values;

public class and_the_scope_has_multiple_owners : given.a_per_value_constraint
{
    async Task Because() => await _storage.ClearValues(_definition);

    [Fact] async Task should_release_the_first_owners_values() => (await _storage.IsAllowed(_otherOwner, _definition, "first")).IsAllowed.ShouldBeTrue();
    [Fact] async Task should_release_the_second_owners_values() => (await _storage.IsAllowed(_owner, _definition, "foreign")).IsAllowed.ShouldBeTrue();
    [Fact] async Task should_preserve_another_scope() => (await _storage.IsAllowed(_otherOwner, _definition, "scoped", "another-scope")).IsAllowed.ShouldBeFalse();
}
