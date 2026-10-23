// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore.Concepts;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Sql.Cluster;
using Cratis.Chronicle.Storage.Sql.EventStores;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.for_ColdStartStorage.given;

public class a_cold_start_database : Specification
{
    protected static readonly EventStoreName Store = "store";
    protected static readonly EventStoreNamespaceName Namespace = "namespace";
    protected IDatabase _database;
    protected EventStoreDbContext _store;
    protected NamespaceDbContext _namespace;
    protected ClusterDbContext _cluster;
    readonly InsertWinner _insertWinner = new();
    readonly RenameWinner _renameWinner = new();
    readonly List<SqliteConnection> _connections = [];

    /// <summary>
    /// Gets whether the contexts use a retrying execution strategy, as SQL Server and PostgreSQL do in production.
    /// </summary>
    protected virtual bool UsesRetryingExecutionStrategy => false;

    async Task Establish()
    {
        var storeOptions = (await Options<EventStoreDbContext>()).AddInterceptors(_insertWinner, _renameWinner);
        var namespaceOptions = (await Options<NamespaceDbContext>()).AddInterceptors(_insertWinner, _renameWinner);
        var clusterOptions = (await Options<ClusterDbContext>()).AddInterceptors(_insertWinner, _renameWinner);
        _store = new(storeOptions.Options);
        _namespace = new(namespaceOptions.Options);
        _cluster = new(clusterOptions.Options);
        await _store.Database.EnsureCreatedAsync();
        await _namespace.Database.EnsureCreatedAsync();
        await _cluster.Database.EnsureCreatedAsync();
        _database = Substitute.For<IDatabase>();
        _database.EventStore(Store).Returns(_ => Task.FromResult(new DbContextScope<EventStoreDbContext>(new(storeOptions.Options), () => { })));
        _database.Namespace(Store, Namespace).Returns(_ => Task.FromResult(new DbContextScope<NamespaceDbContext>(new(namespaceOptions.Options), () => { })));
        _database.Cluster().Returns(_ => Task.FromResult(new DbContextScope<ClusterDbContext>(new(clusterOptions.Options), () => { })));
    }

    protected async Task Race(Func<Task> save, Func<Task>? winner = null, bool rename = false)
    {
        // The losing call has already read a missing row when the second call inserts it.
        // For rename, pause before beginning the write transaction so SQLite does not hold a writer lock.
        var winnerCompleted = false;
        async Task Win()
        {
            await (winner ?? save)();
            winnerCompleted = true;
        }

        if (rename)
        {
            _renameWinner.Arm(Win);
        }
        else
        {
            _insertWinner.Arm(Win);
        }

        await save().WaitAsync(TimeSpan.FromSeconds(30));
        if (!winnerCompleted)
        {
            throw new InvalidOperationException("The competing write did not run before the losing write.");
        }
    }

    async Task<DbContextOptionsBuilder<T>> Options<T>()
        where T : DbContext
    {
        var connection = new SqliteConnection($"DataSource=cold-start-{Guid.NewGuid():N};Mode=Memory;Cache=Shared");
        _connections.Add(connection);
        await connection.OpenAsync();
        return new DbContextOptionsBuilder<T>()
            .UseSqlite(connection.ConnectionString, sqlite =>
            {
                if (UsesRetryingExecutionStrategy)
                {
                    sqlite.ExecutionStrategy(dependencies => new RetryingExecutionStrategy(dependencies));
                }
            })
            .AddConceptAsSupport()
            .WithoutServiceProviderCaching();
    }

    async Task Destroy()
    {
        await _store.DisposeAsync();
        await _namespace.DisposeAsync();
        await _cluster.DisposeAsync();
        foreach (var connection in _connections)
        {
            await connection.DisposeAsync();
        }
    }
}
