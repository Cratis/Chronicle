// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Storage.MongoDB.Sinks;

namespace Cratis.Chronicle.Storage.MongoDB.Events.Constraints.for_UniqueConstraintsStorage.when_getting_for_a_source;

[Collection(MongoDBCollection.Name)]
public class and_values_are_streamed(MongoDBFixture fixture) : given.a_unique_constraints_storage(fixture)
{
    readonly List<string> _values = [];

    async Task Establish()
    {
        _definition = _definition with { Mode = UniqueConstraintMode.PerValue };
        await _storage.Save("source", _definition, 1, "first");
        await _storage.Save("source", _definition, 2, "second");
        await _storage.Save("other", _definition, 3, "other");
    }

    async Task Because()
    {
        await foreach (var value in _storage.GetForStream("source", _definition)) _values.Add(value.Value);
    }

    [Fact] void should_stream_only_the_sources_values() => _values.ShouldContainOnly("first", "second");
}
