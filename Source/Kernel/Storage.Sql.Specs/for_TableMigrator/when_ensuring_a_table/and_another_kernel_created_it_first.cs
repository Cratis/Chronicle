// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.Sinks;

namespace Cratis.Chronicle.Storage.Sql.for_TableMigrator.when_ensuring_a_table;

[Collection(PostgreSqlCollection.Name)]
public class and_another_kernel_created_it_first(PostgreSqlFixture fixture) : given.a_postgresql_table_migrator(fixture)
{
    Exception _error;

    async Task Because() => _error = await Catch.Exception(() => Migrator.EnsureTableMigrated(Table, NewContext(), async (context, _) =>
    {
        // The first creation stands in for another kernel; the second is this one's, failing because the table exists.
        await CreateTable(context);
        await CreateTable(context);
    }));

    [Fact] void should_adopt_the_table() => _error.ShouldBeNull();
    [Fact] async Task should_have_the_table() => (await TableExists()).ShouldBeTrue();
}
