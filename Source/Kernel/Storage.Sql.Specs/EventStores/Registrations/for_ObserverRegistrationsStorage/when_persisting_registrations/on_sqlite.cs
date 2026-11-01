// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Observation.Registrations;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Registrations.for_ObserverRegistrationsStorage.when_persisting_registrations;

public class on_sqlite : ObserverRegistrationsStorageContract
{
    static readonly ServiceProvider _services = new ServiceCollection().BuildServiceProvider();
    string _connectionString;
    string _path;

    async Task Establish()
    {
        _path = Path.Combine(Path.GetTempPath(), $"registrations-{Guid.NewGuid():N}.db");
        _connectionString = new SqliteConnectionStringBuilder { DataSource = _path, Pooling = false }.ConnectionString;
        await using var context = Context();
        await context.Database.MigrateAsync();
    }

    void Destroy()
    {
        File.Delete(_path);
        File.Delete($"{_path}.other");
    }

    [Fact] public async Task should_allow_migrations_to_run_again_without_altering_registrations()
    {
        await Open().TrySave(Registration(), new(0));
        await using var context = Context();
        await context.Database.MigrateAsync();
        (await Open().Get("observer", "event-log"))!.Revision.Value.ShouldEqual(1UL);
    }

    protected override IObserverRegistrationsStorage Open() => new ObserverRegistrationsStorage(
        () => Task.FromResult(new DbContextScope<EventStoreDbContext>(Context(), () => { })), SerializerOptions);

    protected override async Task<IObserverRegistrationsStorage> OpenOtherStore()
    {
        var connectionString = new SqliteConnectionStringBuilder { DataSource = $"{_path}.other", Pooling = false }.ConnectionString;
        await using var context = new EventStoreDbContext(Database.BuildOptions<EventStoreDbContext>(_services, connectionString));
        await context.Database.MigrateAsync();
        return new ObserverRegistrationsStorage(() => Task.FromResult(new DbContextScope<EventStoreDbContext>(new(Database.BuildOptions<EventStoreDbContext>(_services, connectionString)), () => { })), SerializerOptions);
    }

    EventStoreDbContext Context() => new(Database.BuildOptions<EventStoreDbContext>(_services, _connectionString));
}
