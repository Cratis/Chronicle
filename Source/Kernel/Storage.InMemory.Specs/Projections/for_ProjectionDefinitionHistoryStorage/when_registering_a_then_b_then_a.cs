// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.InMemory.Projections.for_ProjectionDefinitionHistoryStorage;

public class when_registering_a_then_b_then_a : given.a_history_storage
{
    async Task Because()
    {
        await _storage.Record(_entry);
        await _storage.Record(_entry with { Digest = new string('B', 64), Definition = _entry.Definition with { IsActive = false } });
        await _storage.Record(_entry with { RecordedAt = _entry.RecordedAt.AddDays(1), RegisteringVersion = "2.0.0" });
        await _storage.Record(_entry with { Projection = "another-projection" });
        await Read();
    }
    [Fact] void should_keep_both_definitions_in_deterministic_order() => _entries.Select(_ => _.Digest.Value).ShouldEqual([new string('A', 64), new string('B', 64)]);
    [Fact] void should_keep_the_original_definition() => _entries[0].Definition.IsActive.ShouldEqual(_entry.Definition.IsActive);
    [Fact] void should_keep_the_original_registration() => (_entries[0] with { Definition = _entry.Definition }).ShouldEqual(_entry);
}
