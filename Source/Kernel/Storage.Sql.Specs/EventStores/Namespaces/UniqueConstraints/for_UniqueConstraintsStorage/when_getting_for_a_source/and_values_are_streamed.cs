// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.UniqueConstraints.for_UniqueConstraintsStorage.when_getting_for_a_source;

public class and_values_are_streamed : given.a_per_value_constraint
{
    readonly List<string> _values = [];

    async Task Establish()
    {
        await _storage.Save(_owner, _definition, 1, "first");
        await _storage.Save(_owner, _definition, 2, "second");
        await _storage.Save(_otherOwner, _definition, 3, "other");
    }

    async Task Because()
    {
        await foreach (var value in _storage.GetForStream(_owner, _definition)) _values.Add(value.Value);
    }

    [Fact] void should_stream_only_the_sources_values() => _values.ShouldContainOnly("first", "second");
}
