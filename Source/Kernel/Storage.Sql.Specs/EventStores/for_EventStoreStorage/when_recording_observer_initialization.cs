// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Arc.EntityFrameworkCore.Concepts;
using Cratis.Chronicle.Storage.Sinks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.for_EventStoreStorage;

public class when_recording_observer_initialization : Specification, IDisposable
{
    SqliteConnection _connection;
    IDatabase _database;
    DbContextOptions<EventStoreDbContext> _options;
    EventStoreStorage _storage;
    bool _before;
    bool _after;
    bool _setup;
    int _markers;

    async Task Establish()
    {
        _connection = new SqliteConnection($"DataSource=observer-initialization-{Guid.NewGuid():N};Mode=Memory;Cache=Shared");
        await _connection.OpenAsync();
        _options = new DbContextOptionsBuilder<EventStoreDbContext>().UseSqlite(_connection).AddConceptAsSupport().Options;
        await using (var context = new EventStoreDbContext(_options))
        {
            await context.Database.MigrateAsync();
        }

        _database = Substitute.For<IDatabase>();
        _database.EventStore("orders").Returns(_ => Task.FromResult(new DbContextScope<EventStoreDbContext>(new(_options), static () => { })));
        _storage = CreateStorage();
        await _storage.MarkInitialized();
        _before = await _storage.AreObserversInitialized();
    }

    async Task Because()
    {
        await _storage.MarkObserversInitialized();
        await _storage.MarkObserversInitialized();
        _after = await CreateStorage().AreObserversInitialized();
        _setup = await CreateStorage().IsInitialized();
        await using var context = new EventStoreDbContext(_options);
        _markers = await context.EventStoreInitialization.CountAsync();
    }

    [Fact] void should_not_treat_store_setup_as_observer_initialization() => _before.ShouldBeFalse();
    [Fact] void should_keep_observer_initialization_across_storage_instances() => _after.ShouldBeTrue();
    [Fact] void should_keep_store_setup() => _setup.ShouldBeTrue();
    [Fact] void should_keep_one_marker_for_each_purpose() => _markers.ShouldEqual(2);

    public void Dispose()
    {
        _connection?.Dispose();
        GC.SuppressFinalize(this);
    }

    EventStoreStorage CreateStorage() => new(
        "orders",
        _database,
        Substitute.For<IInstancesOf<ISinkFactory>>(),
        Substitute.For<Orleans.Storage.IJobsStorage>(),
        new JsonSerializerOptions(),
        Substitute.For<Json.IExpandoObjectConverter>());
}
