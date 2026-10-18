// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Arc.EntityFrameworkCore.Concepts;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Observation;
using Cratis.Chronicle.Storage.Sinks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Observers.for_ObserverStorageCompatibility;

public class when_checking_the_downgrade_floor : ObserverStorageCompatibilityContract, IDisposable
{
    SqliteConnection _storeConnection;
    SqliteConnection _namespaceConnection;
    IDatabase _database;

    async Task Establish()
    {
        _storeConnection = new SqliteConnection("DataSource=:memory:");
        _namespaceConnection = new SqliteConnection("DataSource=:memory:");
        await _storeConnection.OpenAsync();
        await _namespaceConnection.OpenAsync();
        var storeOptions = new DbContextOptionsBuilder<EventStoreDbContext>().UseSqlite(_storeConnection).AddConceptAsSupport().Options;
        var namespaceOptions = new DbContextOptionsBuilder<NamespaceDbContext>().UseSqlite(_namespaceConnection).AddConceptAsSupport().Options;
        await using (var context = new EventStoreDbContext(storeOptions))
        {
            await context.Database.MigrateAsync();
        }

        await using (var context = new NamespaceDbContext(namespaceOptions))
        {
            await context.Database.MigrateAsync();
        }

        _database = Substitute.For<IDatabase>();
        _database.EventStore("orders").Returns(_ => Task.FromResult(new DbContextScope<EventStoreDbContext>(new(storeOptions), static () => { })));
        _database.Namespace("orders", Arg.Any<EventStoreNamespaceName>()).Returns(_ => Task.FromResult(new DbContextScope<NamespaceDbContext>(new(namespaceOptions), static () => { })));
    }

    protected override IEventStoreStorage OpenStore() => new EventStoreStorage(
        "orders", _database, Substitute.For<IInstancesOf<ISinkFactory>>(), Substitute.For<Orleans.Storage.IJobsStorage>(), new JsonSerializerOptions(), Substitute.For<Json.IExpandoObjectConverter>());

    protected override IObserverStateStorage OpenObservers() => new ObserverStateStorage("orders", "default", _database);

    public void Dispose()
    {
        _namespaceConnection?.Dispose();
        _storeConnection?.Dispose();
        GC.SuppressFinalize(this);
    }
}
