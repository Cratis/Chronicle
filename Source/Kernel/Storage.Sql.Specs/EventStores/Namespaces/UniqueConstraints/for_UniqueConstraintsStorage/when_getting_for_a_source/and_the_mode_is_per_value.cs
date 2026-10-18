// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.UniqueConstraints.for_UniqueConstraintsStorage.when_getting_for_a_source;

public class and_the_mode_is_per_value : given.a_per_value_constraint
{
    IEnumerable<UniqueConstraintValue> _values;

    async Task Establish()
    {
        await _storage.Save(_owner, _definition, 1, "first", "scope");
        await _storage.Save(_owner, _definition, 2, "second", "scope");
        await _storage.Save(_otherOwner, _definition, 3, "other", "scope");
        await _storage.Save(_owner, _definition, 4, "elsewhere", "elsewhere");
    }

    async Task Because() => _values = await _storage.GetFor(_owner, _definition, "scope");

    [Fact] void should_read_all_and_only_the_sources_retained_values_in_the_scope() => _values.Select(value => value.Value).ShouldContainOnly("first", "second");
}
