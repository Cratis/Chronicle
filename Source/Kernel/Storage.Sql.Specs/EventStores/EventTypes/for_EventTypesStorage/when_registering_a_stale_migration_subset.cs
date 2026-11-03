// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.Sql.EventStores.EventTypes.for_EventTypesStorage;

public class when_registering_a_stale_migration_subset : given.a_populated_migration_chain
{
    EventTypeDefinition _cold;
    EventType _after;
    bool _changed;
    bool _changedAgain;

    async Task Because()
    {
        _changed = await _storage.Register(_stale);
        _changedAgain = await _storage.Register(_stale);
        _after = await ReadRow();
        _cold = await ReadCold();
    }

    [Fact] Task should_still_upcast_from_one_to_three() => ShouldMigrateAcrossChain(_cold, (1, "one"), (3, "three"));
    [Fact] Task should_still_downcast_from_three_to_one() => ShouldMigrateAcrossChain(_cold, (3, "three"), (1, "one"));
    [Fact] void should_not_report_a_change_when_repeated() => _changedAgain.ShouldBeFalse();
    [Fact] void should_not_report_a_change() => _changed.ShouldBeFalse();
    [Fact] async Task should_not_evict_the_warmed_definition() => (await _storage.GetDefinition(_chain.Id)).ShouldBeSame(_warm);
    [Fact] void should_preserve_the_schema_map() => ShouldRetainSchemas(_cold);
    [Fact] void should_preserve_the_full_bridge_chain() => ShouldRetainBridges(_cold);
    [Fact] void should_leave_migration_json_unchanged() => _after.MigrationsJson.ShouldEqual(_before.MigrationsJson);
    [Fact] void should_preserve_version_history() => _after.MigrationVersionsJson.ShouldEqual(_before.MigrationVersionsJson);
}
