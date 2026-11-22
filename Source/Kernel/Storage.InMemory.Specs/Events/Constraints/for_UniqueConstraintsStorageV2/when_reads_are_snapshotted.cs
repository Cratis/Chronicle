// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Storage.InMemory.Events.Constraints.for_UniqueConstraintsStorageV2;

public class when_reads_are_snapshotted : given.a_seeded_v2_store
{
    IEnumerable<UniqueConstraintValue> _snapshot;
    readonly List<UniqueConstraintValue> _streamed = [];

    async Task Because()
    {
        _snapshot = await _storage.GetFor(_source, _definition, _scope);
        var stream = await _storage.GetForStream(_source, _definition, _scope);
        (await _storage.Save(_source, _definition, 20, _otherValue, _scope, _authority)).IsSuccess.ShouldBeTrue();
        await foreach (var value in stream)
        {
            _streamed.Add(value);
        }
    }

    [Fact] void should_keep_the_enumerable_snapshot() => _snapshot.ShouldEqual([_value]);
    [Fact] void should_keep_the_stream_snapshot() => _streamed.ShouldEqual([_value]);
    [Fact] async Task should_expose_the_new_value_only_to_new_reads() => (await _storage.GetFor(_source, _definition, _scope)).ShouldEqual([_otherValue]);
}
