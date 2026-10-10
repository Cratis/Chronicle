// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.UniqueConstraints.for_UniqueConstraintsStorage.when_removing;

public class and_retained_values_are_scoped : given.a_per_value_constraint
{
    async Task Establish()
    {
        await _storage.Save(_owner, _definition, 42L, "shared", "first");
        await _storage.Save(_owner, _definition, 43L, "shared", "second");
    }

    async Task Because() => await _storage.Remove(_owner, _definition, "first");

    [Fact] async Task should_release_the_selected_scope() => (await _storage.IsAllowed(_otherOwner, _definition, "shared", "first")).IsAllowed.ShouldBeTrue();
    [Fact] async Task should_keep_the_other_scope_reserved() => (await _storage.IsAllowed(_otherOwner, _definition, "shared", "second")).IsAllowed.ShouldBeFalse();
}
