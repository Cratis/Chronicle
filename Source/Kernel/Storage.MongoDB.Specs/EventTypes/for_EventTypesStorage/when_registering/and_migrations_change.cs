// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.EventTypes;
using Cratis.Chronicle.Storage.MongoDB.Sinks;

namespace Cratis.Chronicle.Storage.MongoDB.EventTypes.for_EventTypesStorage.when_registering;

[Collection(MongoDBCollection.Name)]
public class and_migrations_change(MongoDBFixture fixture) : given.migration_versions(fixture)
{
    IReadOnlyDictionary<EventTypeMigrationsVersion, IEnumerable<EventTypeMigrationDefinition>> _versions;

    void Establish()
    {
        _definition = _definition with { Migrations = [Migration("changed")] };
    }

    async Task Because()
    {
        await ((IEventTypesStorage)_storage).Register([new(_definition, EventTypeSource.Code)]);
        _versions = await _storage.GetMigrationsVersions(_id);
    }

    [Fact] void should_preserve_history() => EventTypeMigrationsVersion.For(_versions[_version]).ShouldEqual(_version);
}
