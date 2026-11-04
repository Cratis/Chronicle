// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.Sql.EventStores.EventTypes.for_EventTypesStorage;

public class when_migration_serialization_fails : given.a_populated_migration_chain
{
    Exception _exception;
    EventType _after;

    async Task Because()
    {
        var migration = _chain.Migrations.First() with { Operations = FailingOperations() };
        _exception = await Catch.Exception(() => _storage.Register(_stale with { Migrations = [migration] }));
        _after = await ReadRow();
    }

    [Fact] void should_surface_the_failure() => _exception.ShouldNotBeNull();
    [Fact] void should_not_erase_current_migrations() => _after.MigrationsJson.ShouldEqual(_before.MigrationsJson);
    [Fact] void should_not_touch_historical_versions() => _after.MigrationVersionsJson.ShouldEqual(_before.MigrationVersionsJson);

    static IEnumerable<EventTypeMigrationOperations> FailingOperations() => Enumerable.Range(0, 1).Select<int, EventTypeMigrationOperations>(_ => throw new InvalidOperationException("Cannot serialize migration operations"));
}
