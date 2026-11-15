// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore.Concepts;
using Cratis.Chronicle.Concepts.SequenceQueries;
using Cratis.Chronicle.Storage.SequenceQueries;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.SequenceQueries.for_SequenceQueryStorage.given;

/// <summary>
/// A storage that already holds one query and one folder, both stored under the owner alice.
/// </summary>
public abstract class a_storage_holding_one_query_and_one_folder : Specification
{
    protected ISequenceQueryStorage _storage;
    SqliteConnection _connection;

    protected static Concepts.SequenceQueries.SequenceQueryDefinition Query(string id, string name, string owner) =>
        new(
            id,
            name,
            SequenceQueryScope.User,
            owner,
            string.Empty,
            "default",
            "event-log",
            Concepts.SequenceQueries.SequenceQueryFilter.Empty,
            SequenceQuerySortBy.Occurred,
            Descending: false);

    protected static SequenceQueryFolderDefinition Folder(string id, string owner, string path) =>
        new(id, SequenceQueryScope.User, owner, "default", path);

    async Task Establish()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        await _connection.OpenAsync();
        var options = new DbContextOptionsBuilder<EventStoreDbContext>().UseSqlite(_connection).AddConceptAsSupport().WithoutServiceProviderCaching().Options;
        await using (var context = new EventStoreDbContext(options))
        {
            await context.Database.EnsureCreatedAsync();
        }

        var database = Substitute.For<IDatabase>();
        database.EventStore("store").Returns(_ => Task.FromResult(new DbContextScope<EventStoreDbContext>(new(options), () => { })));
        _storage = new SequenceQueryStorage("store", database);
        await _storage.Insert(Query("query-1", "Stored", "alice"));
        await _storage.InsertFolder(Folder("folder-1", "alice", "stored"));
    }

    void Destroy()
    {
        ((SequenceQueryStorage)_storage).Dispose();
        _connection.Dispose();
    }
}
