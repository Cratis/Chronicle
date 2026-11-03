// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.InMemory.Events.EventTypes.for_EventTypesStorage;

public class when_editing_one_migration_bridge : given.a_populated_migration_chain
{
    EventTypeDefinition _stored;
    IEnumerable<EventTypeId> _changed;
    IReadOnlyDictionary<EventTypeMigrationsVersion, IEnumerable<EventTypeMigrationDefinition>> _versions;

    async Task Because()
    {
        _changed = await RegisterBatch(_stale with { Migrations = [Bridge(1, 2, "'edited'", "'one'")] });
        _stored = await _storage.GetDefinition(_chain.Id);
        await _storage.RecordMigrationsVersion(_chain.Id, EventTypeMigrationsVersion.For(_stored.Migrations), _stored.Migrations);
        _versions = await _storage.GetMigrationsVersions(_chain.Id);
    }

    [Fact] void should_report_the_changed_id() => _changed.ShouldContainOnly(_chain.Id);
    [Fact] void should_keep_the_omitted_bridge() => _stored.Migrations.Select(_ => _.FromGeneration.Value).ShouldContainOnly(1u, 2u);
    [Fact] void should_replace_the_supplied_bridge() => _stored.Migrations.First(_ => _.FromGeneration.Value == 1).UpcastJmesPath["value"].GetValue<string>().ShouldEqual("'edited'");
    [Fact] void should_preserve_the_historical_snapshot() => EventTypeMigrationsVersion.For(_versions[_version]).ShouldEqual(_version);
    [Fact] void should_record_the_full_new_chain() => _versions.ContainsKey(EventTypeMigrationsVersion.For(_stored.Migrations)).ShouldBeTrue();
}
