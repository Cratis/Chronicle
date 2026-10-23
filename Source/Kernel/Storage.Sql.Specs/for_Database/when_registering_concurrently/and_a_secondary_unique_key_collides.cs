// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Reactors;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ReactorDefinition = Cratis.Chronicle.Storage.Sql.EventStores.Reactors.ReactorDefinition;

namespace Cratis.Chronicle.Storage.Sql.for_Database.when_registering_concurrently;

public class and_a_secondary_unique_key_collides : given.a_sqlite_database
{
    Exception _error;

    async Task Because()
    {
        await using var scope = await _database.EventStore("registration-failure");
        await scope.DbContext.Database.ExecuteSqlRawAsync("CREATE UNIQUE INDEX unique_reactor_owner ON Reactors (Owner)");
        await scope.DbContext.Reactors.Upsert(new ReactorDefinition { Id = "first-id", Owner = ReactorOwner.Client });
        _error = await Catch.Exception(() => scope.DbContext.Reactors.Upsert(new ReactorDefinition { Id = "other-id", Owner = ReactorOwner.Client }));
    }

    [Fact] void should_preserve_the_failure() => _error.ShouldBeOfExactType<SqliteException>();
}
