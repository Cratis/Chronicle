// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.MongoDB.Sinks;

namespace Cratis.Chronicle.Storage.MongoDB.EventTypes.for_EventTypesStorage;

[Collection(MongoDBCollection.Name)]
public class when_editing_one_migration_bridge(MongoDBFixture fixture) : given.a_populated_migration_chain(fixture)
{
    EventTypeDefinition _cold;
    IEnumerable<EventTypeId> _changed;
    IReadOnlyDictionary<EventTypeMigrationsVersion, IEnumerable<EventTypeMigrationDefinition>> _versions;

    async Task Because()
    {
        _changed = await RegisterBatch(_stale with { Migrations = [Bridge(1, 2, "'edited'", "'one'")] });
        _cold = await ReadCold();
        await _storage.RecordMigrationsVersion(_chain.Id, EventTypeMigrationsVersion.For(_cold.Migrations), _cold.Migrations);
        _versions = await _storage.GetMigrationsVersions(_chain.Id);
    }

    [Fact] void should_report_the_changed_id() => _changed.ShouldContainOnly(_chain.Id);
    [Fact] void should_keep_the_schema_map() => ShouldRetainSchemas(_cold);
    [Fact] void should_keep_the_omitted_bridge() => EventTypeMigrationsVersion.For(_cold.Migrations.Where(_ => _.FromGeneration.Value == 2)).ShouldEqual(EventTypeMigrationsVersion.For(_chain.Migrations.Skip(1)));
    [Fact] void should_replace_only_the_supplied_endpoint() => _cold.Migrations.Single(_ => _.FromGeneration.Value == 1).UpcastJmesPath["value"].GetValue<string>().ShouldEqual("'edited'");
    [Fact] void should_preserve_the_historical_version() => EventTypeMigrationsVersion.For(_versions[_version]).ShouldEqual(_version);
    [Fact] void should_record_a_new_full_chain_version() => _versions.ContainsKey(EventTypeMigrationsVersion.For(_cold.Migrations)).ShouldBeTrue();
}
