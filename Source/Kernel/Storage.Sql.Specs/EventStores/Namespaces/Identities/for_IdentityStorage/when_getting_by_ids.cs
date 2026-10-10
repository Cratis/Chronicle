// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore.Concepts;
using Cratis.Chronicle.Concepts.Identities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Identities.for_IdentityStorage;

public class when_getting_by_ids : Specification, IDisposable
{
    SqliteConnection _connection;
    string _connectionString;
    IdentityStorage _reader;
    IdentityId _id;
    IReadOnlyDictionary<IdentityId, Concepts.Identities.Identity> _result;

    async Task Establish()
    {
        _connectionString = $"DataSource=identities-{Guid.NewGuid():N};Mode=Memory;Cache=Shared";
        _connection = new SqliteConnection(_connectionString);
        await _connection.OpenAsync();
        await using (var context = CreateContext())
        {
            await context.Database.EnsureCreatedAsync();
        }
        var database = Substitute.For<IDatabase>();
        database.Namespace("store", "tenant").Returns(_ => Task.FromResult(new DbContextScope<NamespaceDbContext>(CreateContext(), () => { })));
        _reader = new IdentityStorage("store", "tenant", database);
        _id = await _reader.GetSingleFor(new Concepts.Identities.Identity("person", "Old name", "username"));
        await _reader.Populate();
        var writer = new IdentityStorage("store", "tenant", database);
        await writer.Rename("person", "Current name");
    }

    async Task Because() => _result = await _reader.GetByIds([_id, IdentityId.New(), _id]);

    [Fact] void should_omit_missing_ids() => _result.Keys.ShouldContainOnly(_id);
    [Fact] void should_bypass_the_stale_reader_cache() => _result[_id].Name.ShouldEqual("Current name");

    public void Dispose()
    {
        _connection.Dispose();
        GC.SuppressFinalize(this);
    }

    NamespaceDbContext CreateContext() => new(new DbContextOptionsBuilder<NamespaceDbContext>().UseSqlite(_connectionString).AddConceptAsSupport().Options);
}
