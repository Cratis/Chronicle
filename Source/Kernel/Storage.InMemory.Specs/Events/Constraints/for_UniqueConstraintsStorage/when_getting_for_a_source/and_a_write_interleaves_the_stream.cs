// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Storage.InMemory.Events.Constraints.for_UniqueConstraintsStorage.when_getting_for_a_source;

public class and_a_write_interleaves_the_stream : given.a_unique_constraints_storage
{
    readonly List<string> _values = [];
    Exception _error;

    async Task Establish()
    {
        _definition = _definition with { Mode = UniqueConstraintMode.PerValue };
        await _storage.Save("source", _definition, 1, "first");
        await _storage.Save("source", _definition, 2, "second");
    }

    async Task Because() => _error = await Catch.Exception(async () =>
    {
        await using var cursor = _storage.GetForStream("source", _definition).GetAsyncEnumerator();
        (await cursor.MoveNextAsync()).ShouldBeTrue();
        _values.Add(cursor.Current.Value);
        await _storage.Save("source", _definition, 3, "later");
        while (await cursor.MoveNextAsync()) _values.Add(cursor.Current.Value);
    });

    [Fact] void should_not_invalidate_the_stream() => _error.ShouldBeNull();
    [Fact] void should_return_only_the_snapshot() => _values.ShouldContainOnly("first", "second");
}
