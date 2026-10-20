// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.Sinks;
using Npgsql;

namespace Cratis.Chronicle.Storage.Sql.for_TableMigrator.when_ensuring_a_table;

[Collection(PostgreSqlCollection.Name)]
public class and_the_upgrade_collides_with_another_kernel(PostgreSqlFixture fixture) : given.a_postgresql_table_migrator(fixture)
{
    Exception _error;
    int _upgrades;

    async Task Establish()
    {
        await using var context = NewContext();
        await CreateTable(context);
    }

    async Task Because() => _error = await Catch.Exception(() => Migrator.EnsureTableMigrated(
        Table,
        NewContext(),
        (_, _) => Task.CompletedTask,
        (_, _) => ++_upgrades == 1
            ? throw new PostgresException("column \"Added\" already exists", "ERROR", "ERROR", PostgresErrorCodes.DuplicateColumn)
            : Task.CompletedTask));

    [Fact] void should_settle_the_upgrade() => _error.ShouldBeNull();
    [Fact] void should_run_the_upgrade_again() => _upgrades.ShouldEqual(2);
}
