// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Projections.for_ProjectionDefinitionHistoryStorage;

public class when_a_sqlite_insert_violates_a_non_unique_constraint : given.a_sqlite_history_storage
{
    Exception _exception;
    async Task Establish()
    {
        await using var scope = await _database.EventStore("history-store");
        await scope.DbContext.Database.ExecuteSqlRawAsync("CREATE TRIGGER reject_history BEFORE INSERT ON ProjectionDefinitionHistory BEGIN SELECT RAISE(ABORT, 'history rejected'); END;");
    }
    async Task Because() => _exception = await Catch.Exception(() => _storage.Record(_entry));
    [Fact] void should_propagate_the_failure() => _exception.ShouldBeOfExactType<DbUpdateException>();
}
