// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore.Concepts;
using Cratis.Chronicle.Concepts;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ObserverSequenceCheckpoints.for_ObserverSequenceCheckpointsStorage.given;

public class a_checkpoint_storage : Specification
{
    protected SqliteConnection _connection;
    protected DbContextOptions<NamespaceDbContext> _options;
    protected IDatabase _database;
    protected ObserverSequenceCheckpointsStorage _storage;

    async Task Establish()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        await _connection.OpenAsync();
        _options = new DbContextOptionsBuilder<NamespaceDbContext>().UseSqlite(_connection).AddConceptAsSupport().WithoutServiceProviderCaching().Options;
        await using var context = new NamespaceDbContext(_options);
        await context.Database.EnsureCreatedAsync();
        _database = Substitute.For<IDatabase>();
        _database.Namespace(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>())
            .Returns(_ => Task.FromResult(new DbContextScope<NamespaceDbContext>(new NamespaceDbContext(_options), () => { })));
        _storage = new("store", "namespace", _database);
    }

    async Task Destroy() => await _connection.DisposeAsync();
}
