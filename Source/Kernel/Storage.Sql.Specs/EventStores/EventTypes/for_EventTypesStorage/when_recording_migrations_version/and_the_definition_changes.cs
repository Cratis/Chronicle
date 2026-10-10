// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.Sql.EventStores.EventTypes.for_EventTypesStorage.when_recording_migrations_version;

public class and_the_definition_changes : given.migration_versions
{
    IReadOnlyDictionary<EventTypeMigrationsVersion, IEnumerable<EventTypeMigrationDefinition>> _versions;

    void Establish()
    {
        _definition = _definition with { Migrations = [Migration("changed")] };
    }

    async Task Because()
    {
        await _storage.RecordMigrationsVersion(_id, EventTypeMigrationsVersion.For(_definition.Migrations), _definition.Migrations);
        _versions = await _storage.GetMigrationsVersions(_id);
    }

    [Fact] void should_keep_both_versions() => _versions.Count.ShouldEqual(2);
    [Fact] void should_keep_the_old_definition() => EventTypeMigrationsVersion.For(_versions[_version]).ShouldEqual(_version);
}
