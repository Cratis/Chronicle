// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.Sql.EventStores.EventTypes.for_EventTypesStorage.when_registering;

public class and_version_matches_after_round_trip : given.migration_versions
{
    EventTypeMigrationsVersion _storedVersion;

    async Task Because()
    {
        _storedVersion = EventTypeMigrationsVersion.For((await _storage.GetDefinition(_id)).Migrations);
    }

    [Fact] void should_match_the_registered_definitions() => _storedVersion.ShouldEqual(_version);
}
