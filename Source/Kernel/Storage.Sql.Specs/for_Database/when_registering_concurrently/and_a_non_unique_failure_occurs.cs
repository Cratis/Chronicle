// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Reactors;
using Microsoft.EntityFrameworkCore;
using ReactorDefinition = Cratis.Chronicle.Storage.Sql.EventStores.Reactors.ReactorDefinition;

namespace Cratis.Chronicle.Storage.Sql.for_Database.when_registering_concurrently;

public class and_a_non_unique_failure_occurs : given.a_sqlite_database
{
    Exception _error;

    async Task Because()
    {
        await using var scope = await _database.EventStore("registration-failure");
        await scope.DbContext.Reactors.Upsert(new ReactorDefinition { Id = "missing-table", Owner = ReactorOwner.Client });

        // Removing the table makes the save fail for a reason that is not a unique-key collision.
        const string dropTable = $"DROP TABLE \"{WellKnownTableNames.ReactorDefinitions}\"";
        await scope.DbContext.Database.ExecuteSqlRawAsync(dropTable);
        _error = await Catch.Exception(() => scope.DbContext.SaveChangesAsync());
    }

    [Fact] void should_preserve_the_failure() => _error.ShouldBeOfExactType<DbUpdateException>();
}
