// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.for_Database.when_enumerating_read_model_metadata_scopes;

public class and_a_scope_was_never_opened_by_this_instance : given.a_sqlite_database
{
    Exception? _error;

    async Task Establish()
    {
        await Execute(_connectionString, "CREATE TABLE EventStores (Name TEXT NOT NULL PRIMARY KEY); INSERT INTO EventStores VALUES ('a+b');");
        var metadata = new SqliteConnectionStringBuilder(_connectionString) { DataSource = Path.Combine(_directory, "cluster+es+a+b.db") }.ConnectionString;
        await Execute(metadata, "CREATE TABLE ReadModelDefinitions (Id TEXT NOT NULL PRIMARY KEY, Name TEXT NOT NULL, SinkType TEXT NOT NULL); INSERT INTO ReadModelDefinitions VALUES ('other-model', 'orders', 'SQL');");
    }

    async Task Because()
    {
        var handle = ReadModelTableHandle.For("a", "b", "requested-model", "orders", ReadModelTableRole.Live, "orders");
        _error = await Catch.Exception(async () =>
        {
            await using var scope = await _database.Tables.Open(handle, [new("id", typeof(string), true, false, false, false)]);
        });
    }

    [Fact] void should_include_the_dormant_scopes_claim() => _error.ShouldBeOfExactType<ReadModelTableNameBindingFailure>();

    static async Task Execute(string connectionString, string sql)
    {
        await using var context = new DbContext(new DbContextOptionsBuilder().UseSqlite(connectionString).Options);
        await context.Database.ExecuteSqlRawAsync(sql);
    }
}
