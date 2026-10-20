// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Arc.EntityFrameworkCore.Concepts;
using Cratis.Chronicle.Storage.Sinks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.for_EventStoreStorage;

public class when_recording_initialization : Specification, IDisposable
{
    SqliteConnection _connection;
    IDatabase _database;
    DbContextOptions<EventStoreDbContext> _options;
    EventStoreStorage _storage;
    bool _wasInitialized;
    bool _isInitializedAfterReconstruction;
    int _markers;

    async Task Establish()
    {
        _connection = new SqliteConnection($"DataSource=initialization-{Guid.NewGuid():N};Mode=Memory;Cache=Shared");
        await _connection.OpenAsync();
        _options = new DbContextOptionsBuilder<EventStoreDbContext>().UseSqlite(_connection).AddConceptAsSupport().WithoutServiceProviderCaching().Options;
        await using (var context = new EventStoreDbContext(_options))
        {
            await context.Database.MigrateAsync();
        }

        _database = Substitute.For<IDatabase>();
        _database.EventStore("orders").Returns(_ => Task.FromResult(new DbContextScope<EventStoreDbContext>(new(_options), static () => { })));
        _storage = CreateStorage();
        _wasInitialized = await _storage.IsInitialized();
    }

    async Task Because()
    {
        await _storage.MarkInitialized();
        await _storage.MarkInitialized();
        _isInitializedAfterReconstruction = await CreateStorage().IsInitialized();
        await using var context = new EventStoreDbContext(_options);
        _markers = await context.EventStoreInitialization.CountAsync();
    }

    [Fact] void should_treat_an_existing_store_without_a_marker_as_uninitialized() => _wasInitialized.ShouldBeFalse();
    [Fact] void should_retain_initialization_across_storage_instances() => _isInitializedAfterReconstruction.ShouldBeTrue();
    [Fact] void should_keep_one_completion_marker() => _markers.ShouldEqual(1);

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
