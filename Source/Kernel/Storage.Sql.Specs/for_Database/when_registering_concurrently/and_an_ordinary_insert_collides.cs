// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Reactors;
using Microsoft.EntityFrameworkCore;
using ReactorDefinition = Cratis.Chronicle.Storage.Sql.EventStores.Reactors.ReactorDefinition;

namespace Cratis.Chronicle.Storage.Sql.for_Database.when_registering_concurrently;

public class and_an_ordinary_insert_collides : given.a_sqlite_database
{
    Exception _error;

    async Task Because()
    {
        var first = new ReactorDefinition { Id = "same-id", Owner = ReactorOwner.Client };
        var second = new ReactorDefinition { Id = "same-id", Owner = ReactorOwner.Client };
        await using var firstScope = await _database.EventStore("registration-failure");
        await using var secondScope = await _database.EventStore("registration-failure");
        firstScope.DbContext.Reactors.Add(first);
        secondScope.DbContext.Reactors.Add(second);
        await firstScope.DbContext.SaveChangesAsync();
        _error = await Catch.Exception(() => secondScope.DbContext.SaveChangesAsync());
    }

    [Fact] void should_preserve_the_failure() => _error.ShouldBeOfExactType<DbUpdateException>();
}
