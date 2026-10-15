// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.UniqueConstraints.for_UniqueConstraintsStorage.when_removing;

public class and_mode_is_per_value : given.a_per_value_constraint
{
    async Task Establish()
    {
        await _storage.Save(_owner, _definition, 42L, "first");
        await _storage.Save(_owner, _definition, 43L, "second");
        await _storage.Save(_otherOwner, _definition, 44L, "foreign");
    }

    async Task Because() => await _storage.Remove(_owner, _definition);

    [Fact] async Task should_release_the_earlier_value() => (await _storage.IsAllowed(_otherOwner, _definition, "first")).IsAllowed.ShouldBeTrue();
    [Fact] async Task should_release_the_later_value() => (await _storage.IsAllowed(_otherOwner, _definition, "second")).IsAllowed.ShouldBeTrue();
    [Fact] async Task should_keep_other_sources_claims() => (await _storage.IsAllowed(_owner, _definition, "foreign")).IsAllowed.ShouldBeFalse();
}
