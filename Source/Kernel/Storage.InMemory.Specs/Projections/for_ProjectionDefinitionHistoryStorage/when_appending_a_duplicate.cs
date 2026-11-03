// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.InMemory.Projections.for_ProjectionDefinitionHistoryStorage;

public class when_appending_a_duplicate : given.a_history_storage
{
    async Task Because()
    {
        await _storage.Record(_entry);
        await _storage.Record(_entry with { RecordedAt = _entry.RecordedAt.AddDays(1), RegisteringVersion = "2.0.0" });
        await Read();
    }
    [Fact] void should_keep_the_original_definition() => _entries[0].Definition.IsActive.ShouldEqual(_entry.Definition.IsActive);
    [Fact] void should_keep_one_entry() => _entries.Length.ShouldEqual(1);
    [Fact] void should_keep_the_first_registration() => (_entries[0] with { Definition = _entry.Definition }).ShouldEqual(_entry);
}
