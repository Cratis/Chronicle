// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.MongoDB.Sinks;

namespace Cratis.Chronicle.Storage.MongoDB.EventTypes.for_EventTypesStorage.when_registering;

[Collection(MongoDBCollection.Name)]
public class and_version_matches_after_round_trip(MongoDBFixture fixture) : given.migration_versions(fixture)
{
    EventTypeMigrationsVersion _storedVersion;

    async Task Because()
    {
        _storedVersion = EventTypeMigrationsVersion.For((await _storage.GetDefinition(_id)).Migrations);
    }

    [Fact] void should_match_the_registered_definitions() => _storedVersion.ShouldEqual(_version);
}
