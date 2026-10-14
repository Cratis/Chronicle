// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Storage.InMemory.Events.Constraints.for_UniqueConstraintsStorage.when_getting_for_a_source;

public class and_the_mode_is_per_event_source : given.a_unique_constraints_storage
{
    IEnumerable<UniqueConstraintValue> _values;

    async Task Establish()
    {
        await _storage.Save("source", _definition, 1, "old", "scope");
        await _storage.Save("source", _definition, 2, "current", "scope");
        await _storage.Save("other", _definition, 3, "other", "scope");
        await _storage.Save("source", _definition, 4, "different-scope", "elsewhere");
    }

    async Task Because() => _values = await _storage.GetFor("source", _definition, "scope");

    [Fact] void should_read_only_the_sources_current_value_in_the_requested_scope() => _values.Select(value => value.Value).ShouldContainOnly("current");
}
