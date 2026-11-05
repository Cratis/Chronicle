// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore.Concepts;
using Cratis.Chronicle.Concepts.SequenceQueries;
using Cratis.Chronicle.Storage.Sql.Sinks;
using Microsoft.EntityFrameworkCore;

#pragma warning disable CA2100 // Fixture-owned values only.

namespace Cratis.Chronicle.Storage.Sql.EventStores.SequenceQueries.for_SequenceQueryStorage.given;

/// <summary>
/// Two spellings of a saved query owner that the default collation of SQL Server treats as one.
/// </summary>
/// <param name="fixture">The SQL Server fixture.</param>
/// <param name="first">The first spelling.</param>
/// <param name="second">The second spelling.</param>
public abstract class two_owner_spellings(SqlServerFixture fixture, string first, string second) : Specification
{
    protected string _first = first;
    protected string _second = second;
    protected long _collapsedByCollation;
    protected string[] _queriesForFirst = [];
    protected string[] _foldersForFirst = [];
    string _connectionString = string.Empty;

    protected async Task SaveAndRead()
    {
        _connectionString = await fixture.CreateDatabase();
        var options = new DbContextOptionsBuilder<EventStoreDbContext>().UseSqlServer(_connectionString).AddConceptAsSupport().WithoutServiceProviderCaching().Options;
        await using (var context = new EventStoreDbContext(options))
        {
            await context.Database.EnsureCreatedAsync();
            _collapsedByCollation = await Collapsed(context);
        }

        var database = Substitute.For<IDatabase>();
        database.EventStore("store").Returns(_ => Task.FromResult(new DbContextScope<EventStoreDbContext>(new(options), () => { })));
        using var storage = new SequenceQueryStorage("store", database);

        await storage.Save(Query("query-own", SequenceQueryScope.User, _first));
        await storage.Save(Query("query-other", SequenceQueryScope.User, _second));
        await storage.Save(Query("query-shared", SequenceQueryScope.Everyone, "someone-else"));
        await storage.SaveFolder(Folder("folder-own", SequenceQueryScope.User, _first, "own"));
        await storage.SaveFolder(Folder("folder-other", SequenceQueryScope.User, _second, "other"));
        await storage.SaveFolder(Folder("folder-shared", SequenceQueryScope.Everyone, "someone-else", "shared"));

        _queriesForFirst = (await storage.GetAllFor(_first)).Select(_ => _.Id.Value).Order().ToArray();
        _foldersForFirst = (await storage.GetAllFoldersFor(_first)).Select(_ => _.Id.Value).Order().ToArray();
    }

    protected void Destroy() => fixture.DropDatabase(_connectionString).GetAwaiter().GetResult();

    static Concepts.SequenceQueries.SequenceQueryDefinition Query(string id, SequenceQueryScope scope, string owner) =>
        new(
            id,
            id,
            scope,
            owner,
            string.Empty,
            "default",
            "event-log",
            Concepts.SequenceQueries.SequenceQueryFilter.Empty,
            SequenceQuerySortBy.Occurred,
            Descending: false);

    static SequenceQueryFolderDefinition Folder(string id, SequenceQueryScope scope, string owner, string path) =>
        new(id, scope, owner, "default", path);

    async Task<long> Collapsed(EventStoreDbContext context)
    {
        await using var command = context.Database.GetDbConnection().CreateCommand();
        command.CommandText = $"SELECT CASE WHEN N'{_first}' = N'{_second}' THEN 1 ELSE 0 END";
        await context.Database.OpenConnectionAsync();
        return Convert.ToInt64(await command.ExecuteScalarAsync());
    }
}
