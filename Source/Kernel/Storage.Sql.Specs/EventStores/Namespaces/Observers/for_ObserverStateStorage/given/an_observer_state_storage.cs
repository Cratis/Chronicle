// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore.Concepts;
using Cratis.Chronicle.Concepts;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Observers.for_ObserverStateStorage.given;

/// <summary>
/// Sets up an <see cref="ObserverStateStorage"/> backed by a shared in-memory SQLite database.
/// A single open connection is shared across every <see cref="IDatabase.Namespace"/> scope so the
/// in-memory schema and data survive between the storage's individual unit-of-work scopes.
/// </summary>
/// <remarks>
/// Every <see cref="IDatabase.Namespace"/> call awaits <see cref="_load"/> before handing back a
/// scope. The gate starts open, so storage operations resolve immediately by default; a spec can
/// close it (assign a fresh, incomplete <see cref="TaskCompletionSource"/>) to hold the asynchronous
/// load open and deterministically control when it completes relative to subscription.
/// </remarks>
public class an_observer_state_storage : Specification, IDisposable
{
    protected static readonly EventStoreName _eventStore = "test-store";
    protected static readonly EventStoreNamespaceName _namespace = "test-namespace";
    protected SqliteConnection _connection;
    protected IDatabase _database;
    protected ObserverStateStorage _storage;
    protected TaskCompletionSource _load;

    void Establish()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        using (var schemaContext = CreateContext())
        {
            schemaContext.Database.EnsureCreated();
        }

        _load = new TaskCompletionSource();
        _load.SetResult();

        _database = Substitute.For<IDatabase>();
        _database.Namespace(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>())
            .Returns(_ => CreateScopeWhenLoaded());

        _storage = new ObserverStateStorage(_eventStore, _namespace, _database);
    }

    protected NamespaceDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<NamespaceDbContext>()
            .UseSqlite(_connection)
            .AddConceptAsSupport()
            .Options;

        return new NamespaceDbContext(options);
    }

    async Task<DbContextScope<NamespaceDbContext>> CreateScopeWhenLoaded()
    {
        await _load.Task;
        return new DbContextScope<NamespaceDbContext>(CreateContext(), () => { });
    }

    public void Dispose()
    {
        _connection.Dispose();
        GC.SuppressFinalize(this);
    }
}
