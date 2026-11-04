// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.Sql.EventStores.EventTypes.for_EventTypesStorage;

public class when_registering_unchanged_metadata : given.a_populated_migration_chain
{
    EventType _after;
    bool _changed;

    protected virtual Task<bool> RegisterUnchanged() => _storage.Register(new(_chain.Id, 1), _chain.Generations.First().Schema, source: EventTypeSource.Unknown);

    async Task Because()
    {
        _changed = await RegisterUnchanged();
        _after = await ReadRow();
    }

    [Fact] void should_not_report_a_change() => _changed.ShouldBeFalse();
    [Fact] async Task should_not_evict_the_definition() => (await _storage.GetDefinition(_chain.Id)).ShouldBeSame(_warm);
    [Fact] async Task should_keep_the_warmed_latest_schema() => (await _storage.GetFor(_chain.Id, 3)).Schema.ToJson().ShouldEqual(_warmSchema.Schema.ToJson());
    [Fact] void should_keep_the_explicit_visibility_when_unspecified() => _after.Visibility.ShouldEqual(EventTypeVisibility.Public);
    [Fact] void should_not_rewrite_migrations() => _after.MigrationsJson.ShouldEqual(_before.MigrationsJson);
    [Fact] void should_not_touch_history() => _after.MigrationVersionsJson.ShouldEqual(_before.MigrationVersionsJson);
}
