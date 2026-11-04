// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Sql.Sinks;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Projections.for_ProjectionDefinitionHistoryStorage;

[Collection(PostgreSqlCollection.Name)]
public class when_recording_postgresql_definitions(PostgreSqlFixture fixture) : given.a_postgresql_history_storage(fixture)
{
    async Task Because()
    {
        await _storage.Record(_entry);
        await _storage.Record(_entry with { Digest = new string('B', 64), Definition = _entry.Definition with { IsActive = false } });
        await _storage.Record(_entry with { RegisteringVersion = "2.0.0", RecordedAt = _entry.RecordedAt.AddDays(1) });
        await _storage.Record(_entry with { Projection = "another-projection" });
        await Read();
    }
    [Fact] void should_keep_one_snapshot_per_digest_in_order() => _entries.Select(_ => _.Digest.Value).ShouldEqual([new string('A', 64), new string('B', 64)]);
    [Fact] void should_keep_the_first_version() => _entries[0].RegisteringVersion.ShouldEqual("1.0.0");
    [Fact] void should_keep_the_first_timestamp() => _entries[0].RecordedAt.ShouldEqual(_entry.RecordedAt);
    [Fact] void should_round_trip_the_original_version() => _entries[0].Definition.Version.ShouldEqual("1.0.0");
    [Fact] void should_preserve_the_original_definition_timestamp() => _entries[0].Definition.LastUpdated.ShouldEqual(_entry.Definition.LastUpdated);
    [Fact] void should_preserve_pinned_delivery() => _entries[0].Definition.GenerationDelivery.ShouldEqual(EventGenerationDelivery.Pinned);
    [Fact] void should_preserve_both_event_generations() => _entries[0].Definition.From.Keys.ShouldContainOnly(new EventType("event", 1), new EventType("event", 2));
    [Fact] void should_keep_the_second_snapshot() => _entries[1].Definition.IsActive.ShouldBeFalse();
}
