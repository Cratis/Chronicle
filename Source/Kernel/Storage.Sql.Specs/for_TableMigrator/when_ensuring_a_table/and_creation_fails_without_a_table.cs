// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.Sinks;
using Npgsql;

namespace Cratis.Chronicle.Storage.Sql.for_TableMigrator.when_ensuring_a_table;

[Collection(PostgreSqlCollection.Name)]
public class and_creation_fails_without_a_table(PostgreSqlFixture fixture) : given.a_postgresql_table_migrator(fixture)
{
    readonly PostgresException _failure = new("relation \"another_table\" already exists", "ERROR", "ERROR", PostgresErrorCodes.DuplicateTable);
    Exception _error;

    async Task Because() => _error = await Catch.Exception(() => Migrator.EnsureTableMigrated(Table, NewContext(), (_, _) => throw _failure));

    [Fact] void should_not_swallow_the_failure() => _error.ShouldEqual(_failure);
}
