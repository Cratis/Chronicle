// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels.for_ReadModelTableNameBindings.when_bootstrapping;

public class and_a_user_relation_occupies_the_metadata_name : given.a_sqlite_ledger
{
    Exception _error;

    async Task Establish()
    {
        await _context.Database.ExecuteSqlRawAsync("CREATE TABLE chronicle_read_model_table_names (UserData TEXT NOT NULL)");
        await _context.Database.ExecuteSqlRawAsync("INSERT INTO chronicle_read_model_table_names VALUES ('preserved')");
    }

    async Task Because() => _error = await Catch.Exception(() => Reserve(_owner, "orders"));

    [Fact] void should_refuse_the_incompatible_schema() => _error.ShouldBeOfExactType<ReadModelTableBindingsSchemaMismatch>();
    [Fact] async Task should_keep_the_old_rows() => (await _context.Database.SqlQueryRaw<string>("SELECT UserData AS Value FROM chronicle_read_model_table_names").SingleAsync()).ShouldEqual("preserved");
    [Fact] async Task should_not_create_a_user_table() => (await _context.Database.SqlQueryRaw<long>("SELECT count(*) AS Value FROM sqlite_schema WHERE name = 'orders'").SingleAsync()).ShouldEqual(0L);
}
