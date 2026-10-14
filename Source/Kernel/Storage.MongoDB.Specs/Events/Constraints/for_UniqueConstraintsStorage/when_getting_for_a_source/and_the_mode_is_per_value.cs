// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Storage.MongoDB.Sinks;

namespace Cratis.Chronicle.Storage.MongoDB.Events.Constraints.for_UniqueConstraintsStorage.when_getting_for_a_source;

[Collection(MongoDBCollection.Name)]
public class and_the_mode_is_per_value(MongoDBFixture fixture) : given.a_unique_constraints_storage(fixture)
{
    IEnumerable<UniqueConstraintValue> _values;

    async Task Establish()
    {
        _definition = _definition with { Mode = UniqueConstraintMode.PerValue };
        await _storage.Save("source", _definition, 1, "first", "scope");
        await _storage.Save("source", _definition, 2, "second", "scope");
        await _storage.Save("other", _definition, 3, "other", "scope");
        await _storage.Save("source", _definition, 4, "elsewhere", "elsewhere");
    }

    async Task Because() => _values = await _storage.GetFor("source", _definition, "scope");

    [Fact] void should_read_all_and_only_the_sources_retained_values_in_the_scope() => _values.Select(value => value.Value).ShouldContainOnly("first", "second");
}
