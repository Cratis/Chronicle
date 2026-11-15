// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore.Concepts;
using Cratis.Chronicle.Concepts.SequenceQueries;
using Cratis.Chronicle.Storage.Sql.Sinks;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.SequenceQueries.for_SequenceQueryStorage;

[Collection(SqlServerCollection.Name)]
public class when_getting_by_identifier(SqlServerFixture fixture) : Specification
{
    string _connectionString = string.Empty;
    Concepts.SequenceQueries.SequenceQueryDefinition? _query;
    Concepts.SequenceQueries.SequenceQueryDefinition? _missingQuery;
    SequenceQueryFolderDefinition? _folder;
    SequenceQueryFolderDefinition? _missingFolder;

    async Task Because()
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

        await storage.Insert(new Concepts.SequenceQueries.SequenceQueryDefinition(
            "query-1",
            "Stored",
            SequenceQueryScope.Everyone,
            "Alice",
            string.Empty,
            "default",
            "event-log",
            Concepts.SequenceQueries.SequenceQueryFilter.Empty,
            SequenceQuerySortBy.Occurred,
            Descending: false));
        await storage.InsertFolder(new SequenceQueryFolderDefinition("folder-1", SequenceQueryScope.User, "Alice", "default", "stored"));

        _query = await storage.Get("query-1");
        _missingQuery = await storage.Get("query-2");
        _folder = await storage.GetFolder("folder-1");
        _missingFolder = await storage.GetFolder("folder-2");
    }

    void Destroy() => fixture.DropDatabase(_connectionString).GetAwaiter().GetResult();

    [Fact] void should_get_the_query_with_the_owner_exactly_as_stored() => _query!.Owner.Value.ShouldEqual("Alice");
    [Fact] void should_get_nothing_for_a_missing_query() => _missingQuery.ShouldBeNull();
    [Fact] void should_get_the_folder_with_the_owner_exactly_as_stored() => _folder!.Owner.Value.ShouldEqual("Alice");
    [Fact] void should_get_nothing_for_a_missing_folder() => _missingFolder.ShouldBeNull();
}
