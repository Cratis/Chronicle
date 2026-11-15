// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Reactive.Linq;
using Cratis.Arc.EntityFrameworkCore.Concepts;
using Cratis.Chronicle.Concepts;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using KernelObserverDefinition = Cratis.Chronicle.Storage.Observation.ObserverDefinition;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Observers.for_ObserverDefinitionsStorage.given;

public class an_observing_observer_definitions_storage : Specification
{
    protected ObserverDefinitionsStorage _storage;
    protected ConcurrentQueue<IEnumerable<KernelObserverDefinition>> _emissions = new();
    protected readonly SemaphoreSlim _scopesReleased = new(0);
    protected IDisposable _subscription;
    protected Action _onScopeCompleted;
    protected int _completedScopes;
    readonly SemaphoreSlim _connectionGate = new(1, 1);
    readonly SemaphoreSlim _emitted = new(0);
    SqliteConnection _connection;

    void Establish()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        using var context = CreateContext();
        context.Database.EnsureCreated();
        var database = Substitute.For<IDatabase>();
        database.LiveQueryPollingInterval.Returns(TimeSpan.FromMilliseconds(50));
        database.EventStore(Arg.Any<EventStoreName>()).Returns(_ => CreateSerializedScope());
        _storage = new ObserverDefinitionsStorage("test-store", database);
    }

    protected async Task Seed(string id)
    {
        await using var scope = await CreateSerializedScope();
        scope.DbContext.Observers.Add(new ObserverDefinition
        {
            Id = id,
            EventTypes = [new("event", 2, "key")],
            Type = Concepts.Observation.ObserverType.Reactor,
            Owner = Concepts.Observation.ObserverOwner.Client,
            GenerationDelivery = Concepts.Observation.EventGenerationDelivery.Pinned
        });
        await scope.DbContext.SaveChangesAsync();
    }

    protected async Task Subscribe()
    {
        var subject = _storage.ObserveAll();
        _subscription = await Task.Run(() => subject.Subscribe(definitions =>
        {
            _emissions.Enqueue(definitions);
            _emitted.Release();
        }));
        await WaitForEmission();
    }

    protected async Task WaitForEmission() => (await _emitted.WaitAsync(TimeSpan.FromSeconds(5))).ShouldBeTrue();

    protected async Task WaitForCompletedScopes(int count)
    {
        while (_scopesReleased.Wait(0))
        {
        }
        for (var i = 0; i < count; i++)
        {
            (await _scopesReleased.WaitAsync(TimeSpan.FromSeconds(5))).ShouldBeTrue();
        }
    }

    EventStoreDbContext CreateContext() => new(new DbContextOptionsBuilder<EventStoreDbContext>()
        .UseSqlite(_connection).AddConceptAsSupport().WithoutServiceProviderCaching().Options);

    async Task<DbContextScope<EventStoreDbContext>> CreateSerializedScope()
    {
        await _connectionGate.WaitAsync();
        return new DbContextScope<EventStoreDbContext>(CreateContext(), () =>
        {
            _connectionGate.Release();
            Interlocked.Increment(ref _completedScopes);
            _scopesReleased.Release();
            _onScopeCompleted?.Invoke();
        });
    }

    void Destroy()
    {
        _subscription?.Dispose();
        _connection.Dispose();
    }
}
