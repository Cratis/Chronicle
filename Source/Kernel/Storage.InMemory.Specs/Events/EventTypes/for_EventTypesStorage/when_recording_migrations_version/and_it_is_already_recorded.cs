// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.InMemory.Events.EventTypes.for_EventTypesStorage.when_recording_migrations_version;

public class and_it_is_already_recorded : given.migration_versions
{
    IReadOnlyDictionary<EventTypeMigrationsVersion, IEnumerable<EventTypeMigrationDefinition>> _versions;

    async Task Because()
    {
        await _storage.RecordMigrationsVersion(_id, _version, _definition.Migrations);
        _versions = await _storage.GetMigrationsVersions(_id);
    }

    [Fact] void should_record_only_one_version() => _versions.Count.ShouldEqual(1);
    [Fact] void should_keep_the_executable_definition() => EventTypeMigrationsVersion.For(_versions[_version]).ShouldEqual(_version);
}
