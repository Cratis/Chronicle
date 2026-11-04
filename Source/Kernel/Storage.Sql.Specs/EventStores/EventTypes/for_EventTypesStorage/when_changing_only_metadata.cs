// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.EventTypes;

namespace Cratis.Chronicle.Storage.Sql.EventStores.EventTypes.for_EventTypesStorage;

public abstract class when_changing_only_metadata : given.a_populated_migration_chain
{
    protected EventType _after;
    EventTypeDefinition _cold;
    bool _changed;
    bool _changedAgain;

    protected abstract Task<bool> RegisterMetadata();

    protected async Task<bool> RegisterMetadataBatch(EventTypeToRegister registration)
    {
        var definition = registration.Definition with { Generations = [_chain.Generations.First()], Migrations = [] };
        var changed = await ((IEventTypesStorage)_storage).Register([registration with { Definition = definition }]);
        return changed.Any();
    }

    async Task Because()
    {
        _changed = await RegisterMetadata();
        _changedAgain = await RegisterMetadata();
        _after = await ReadRow();
        _cold = await ReadCold();
    }

    [Fact] async Task should_keep_the_latest_schema_available() => (await _storage.GetFor(_chain.Id, 3)).Schema.ToJson().ShouldEqual(_chain.Generations.Last().Schema.ToJson());
    [Fact] async Task should_keep_the_latest_generation() => (await _storage.GetLatestForAllEventTypes()).Single(_ => _.Type.Id == _chain.Id).Type.Generation.Value.ShouldEqual(3u);
    [Fact] void should_report_a_metadata_change() => _changed.ShouldBeTrue();
    [Fact] void should_not_report_it_twice() => _changedAgain.ShouldBeFalse();
    [Fact] void should_keep_all_schemas() => ShouldRetainSchemas(_cold);
    [Fact] void should_keep_all_bridges() => ShouldRetainBridges(_cold);
    [Fact] void should_not_rewrite_migration_json() => _after.MigrationsJson.ShouldEqual(_before.MigrationsJson);
    [Fact] void should_preserve_historical_versions() => _after.MigrationVersionsJson.ShouldEqual(_before.MigrationVersionsJson);
    [Fact] async Task should_evict_the_warmed_definition() => (await _storage.GetDefinition(_chain.Id)).ShouldNotBeSame(_warm);
}
