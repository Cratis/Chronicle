// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.Sinks;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Projections.for_ProjectionDefinitionHistoryStorage;

[Collection(PostgreSqlCollection.Name)]
public class when_a_postgresql_insert_violates_a_non_unique_constraint(PostgreSqlFixture fixture) : given.a_postgresql_history_storage(fixture)
{
    Exception _exception;
    async Task Establish()
    {
        await using var scope = await Database.EventStore(Store);
        await scope.DbContext.Database.ExecuteSqlRawAsync("ALTER TABLE \"ProjectionDefinitionHistory\" ADD CONSTRAINT reject_history CHECK (\"DigestAlgorithm\" = 'another-algorithm');");
    }
    async Task Because() => _exception = await Catch.Exception(() => _storage.Record(_entry));
    [Fact] void should_propagate_the_failure() => _exception.ShouldBeOfExactType<DbUpdateException>();
}
