// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore.Concepts;
using Cratis.Chronicle.Concepts.SequenceQueries;
using Cratis.Chronicle.Storage.SequenceQueries;
using Cratis.Chronicle.Storage.Sql.Sinks;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.SequenceQueries.for_SequenceQueryStorage.given;

/// <summary>
/// A SQL Server storage holding a query and a folder under one owner, changed with another spelling of that owner
/// that the default collation of the database treats as the same.
/// </summary>
/// <param name="fixture">The SQL Server fixture.</param>
/// <param name="stored">The owner the items are stored under.</param>
/// <param name="expected">The other spelling the changes are made with.</param>
public abstract class a_stored_owner_and_a_differently_spelled_one(SqlServerFixture fixture, string stored, string expected) : Specification
{
    protected SequenceQueryWriteOutcome _updatedQuery;
    protected SequenceQueryWriteOutcome _updatedFolder;
    protected SequenceQueryWriteOutcome _deletedQuery;
    protected SequenceQueryWriteOutcome _deletedFolder;
    protected Concepts.SequenceQueries.SequenceQueryDefinition? _query;
    protected SequenceQueryFolderDefinition? _folder;
    string _connectionString = string.Empty;

    protected async Task ChangeAndRead()
    {
        _connectionString = await fixture.CreateDatabase();
        var options = new DbContextOptionsBuilder<EventStoreDbContext>().UseSqlServer(_connectionString).AddConceptAsSupport().WithoutServiceProviderCaching().Options;
        await using (var context = new EventStoreDbContext(options))
        {
            await context.Database.EnsureCreatedAsync();
        }

        var database = Substitute.For<IDatabase>();
        database.EventStore("store").Returns(_ => Task.FromResult(new DbContextScope<EventStoreDbContext>(new(options), () => { })));
        using var storage = new SequenceQueryStorage("store", database);

        await storage.Insert(Query("Stored", stored));
        await storage.InsertFolder(new SequenceQueryFolderDefinition("folder-1", SequenceQueryScope.User, stored, "default", "stored"));

        _updatedQuery = await storage.Update(Query("Changed", expected), expected);
        _updatedFolder = await storage.UpdateFolder(new SequenceQueryFolderDefinition("folder-1", SequenceQueryScope.User, expected, "default", "changed"), expected);
        _deletedQuery = await storage.Delete("query-1", expected);
        _deletedFolder = await storage.DeleteFolder("folder-1", expected);
        _query = await storage.Get("query-1");
        _folder = await storage.GetFolder("folder-1");
    }

    protected void Destroy() => fixture.DropDatabase(_connectionString).GetAwaiter().GetResult();

    static Concepts.SequenceQueries.SequenceQueryDefinition Query(string name, string owner) =>
        new(
            "query-1",
            name,
            SequenceQueryScope.User,
            owner,
            string.Empty,
            "default",
            "event-log",
            Concepts.SequenceQueries.SequenceQueryFilter.Empty,
            SequenceQuerySortBy.Occurred,
            Descending: false);
}
