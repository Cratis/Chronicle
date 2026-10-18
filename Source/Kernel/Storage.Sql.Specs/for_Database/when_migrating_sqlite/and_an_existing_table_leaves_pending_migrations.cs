// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.for_Database.when_migrating_sqlite;

public class and_an_existing_table_leaves_pending_migrations : given.a_sqlite_database
{
    Exception _first;
    Exception _second;

    async Task Establish()
    {
        await using var context = Context();
        await context.Database.ExecuteSqlRawAsync("CREATE TABLE FailedPartitions (Id INTEGER)");
    }

    async Task Because()
    {
        await using var first = Context();
        _first = await Catch.Exception(() => Migrate(first));
        await using var second = Context();
        _second = await Catch.Exception(() => Migrate(second));
    }

    [Fact] void should_refuse_an_incomplete_schema() => _first.ShouldBeOfExactType<DatabaseMigrationIncomplete>();
    [Fact] void should_not_cache_a_failed_migration() => _second.ShouldBeOfExactType<DatabaseMigrationIncomplete>();
}
