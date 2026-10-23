// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Data.Common;
using System.Text.Json;
using Cratis.Arc.EntityFrameworkCore.Concepts;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Sinks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

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
                ? new ClusterDbContext(Options(new PauseBeforeInsert(_firstReady, _releaseFirst)))
                : CreateContext(),
            () => { })));
        _storage = new ClusterStorage(
            _database,
            Substitute.For<IInstancesOf<ISinkFactory>>(),
            Substitute.For<Orleans.Storage.IJobsStorage>(),
            new JsonSerializerOptions(),
            Substitute.For<Json.IExpandoObjectConverter>());
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

    DbContextOptions<ClusterDbContext> Options(params IInterceptor[] interceptors) => new DbContextOptionsBuilder<ClusterDbContext>()
        .UseSqlite(_connectionString)
        .AddConceptAsSupport().WithoutServiceProviderCaching()
        .AddInterceptors(interceptors)
        .Options;

    ClusterDbContext CreateContext() => new(Options());

    public void Dispose()
    {
        _releaseFirst.TrySetResult();
        _storage?.Dispose();
        _anchor?.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Holds the first client's native upsert after it has decided to write, so the second
    /// client stores the same name first. The held statement must then succeed without a duplicate.
    /// </summary>
    sealed class PauseBeforeInsert(TaskCompletionSource ready, TaskCompletionSource release) : DbCommandInterceptor
    {
        public override async ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
            DbCommand command, CommandEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            await Pause(command, cancellationToken);
            return result;
        }

        public override async ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        {
            await Pause(command, cancellationToken);
            return result;
        }

        async Task Pause(DbCommand command, CancellationToken cancellationToken)
        {
            if (!command.CommandText.Contains("INSERT", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            ready.TrySetResult();
            await release.Task.WaitAsync(TimeSpan.FromSeconds(10), cancellationToken);
        }
    }
}
