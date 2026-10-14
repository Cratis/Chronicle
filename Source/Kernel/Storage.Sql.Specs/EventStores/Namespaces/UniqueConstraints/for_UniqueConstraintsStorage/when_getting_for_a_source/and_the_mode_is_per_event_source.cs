// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.UniqueConstraints.for_UniqueConstraintsStorage.when_getting_for_a_source;

public class and_the_mode_is_per_event_source : given.a_unique_constraints_storage
{
    IEnumerable<UniqueConstraintValue> _values;

    async Task Establish()
    {
        await _storage.Save("source", _definition, 1, "old");
        await _storage.Save("source", _definition, 2, "current");
        await _storage.Save("other", _definition, 3, "other");
    }

    async Task Because() => _values = await _storage.GetFor("source", _definition);

    [Fact] void should_read_only_the_sources_current_value() => _values.Select(value => value.Value).ShouldContainOnly("current");
}
