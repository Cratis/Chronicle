// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Arc.EntityFrameworkCore.Concepts;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Sinks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.Cluster.for_ClusterStorage;

public class when_two_clients_ensure_the_same_event_store : Specification, IDisposable
{
    readonly TaskCompletionSource _firstReady = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _releaseFirst = new(TaskCreationOptions.RunContinuationsAsynchronously);
    SqliteConnection _anchor;
    ClusterStorage _storage;
    IDatabase _database;
    string _connectionString;
    IEnumerable<EventStoreName> _stores;

    void Establish()
    {
        _connectionString = $"Data Source=store-{Guid.NewGuid():N};Mode=Memory;Cache=Shared";
        _anchor = new SqliteConnection(_connectionString);
        _anchor.Open();
        using (var context = CreateContext())
        {
            context.Database.EnsureCreated();
        }

        _database = Substitute.For<IDatabase>();
        var count = 0;
        _database.Cluster().Returns(_ => Task.FromResult(new DbContextScope<ClusterDbContext>(
            Interlocked.Increment(ref count) == 1
                ? new PausedClusterDbContext(Options(), _firstReady, _releaseFirst)
                : CreateContext(),
            () => { })));
        _storage = new ClusterStorage(
            _database,
            Substitute.For<IInstancesOf<ISinkFactory>>(),
            Substitute.For<Cratis.Orleans.Storage.IJobsStorage>(),
            new JsonSerializerOptions());
    }

    async Task Because()
    {
        var first = _storage.SaveEventStore("orders");
        await _firstReady.Task.WaitAsync(TimeSpan.FromSeconds(10));
        await _storage.SaveEventStore("orders").WaitAsync(TimeSpan.FromSeconds(10));
        _releaseFirst.SetResult();
        await first.WaitAsync(TimeSpan.FromSeconds(10));
        _stores = await _storage.GetEventStores();
    }

    [Fact] void should_store_the_event_store_only_once() => _stores.Count().ShouldEqual(1);
    [Fact] void should_store_the_requested_name() => _stores.Single().ShouldEqual((EventStoreName)"orders");

    DbContextOptions<ClusterDbContext> Options() => new DbContextOptionsBuilder<ClusterDbContext>()
        .UseSqlite(_connectionString)
        .AddConceptAsSupport()
        .Options;

    ClusterDbContext CreateContext() => new(Options());

    public void Dispose()
    {
        _releaseFirst.TrySetResult();
        _storage?.Dispose();
        _anchor?.Dispose();
        GC.SuppressFinalize(this);
    }

    sealed class PausedClusterDbContext(
        DbContextOptions<ClusterDbContext> options,
        TaskCompletionSource ready,
        TaskCompletionSource release) : ClusterDbContext(options)
    {
        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            ready.TrySetResult();
            await release.Task.WaitAsync(TimeSpan.FromSeconds(10), cancellationToken);
            return await base.SaveChangesAsync(cancellationToken);
        }
    }
}
