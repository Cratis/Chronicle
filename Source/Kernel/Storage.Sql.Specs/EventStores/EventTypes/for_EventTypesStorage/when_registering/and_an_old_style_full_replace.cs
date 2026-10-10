// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.Sql.EventStores.EventTypes.for_EventTypesStorage.when_registering;

public class and_an_old_style_full_replace : given.migration_versions
{
    IReadOnlyDictionary<EventTypeMigrationsVersion, IEnumerable<EventTypeMigrationDefinition>> _versions;

    void Establish()
    {
        _definition = _definition with { Migrations = [Migration("changed")] };
    }

    async Task Because()
    {
        await _storage.Register(_definition);
        _versions = await _storage.GetMigrationsVersions(_id);
    }

    [Fact] void should_preserve_history() => EventTypeMigrationsVersion.For(_versions[_version]).ShouldEqual(_version);
}
