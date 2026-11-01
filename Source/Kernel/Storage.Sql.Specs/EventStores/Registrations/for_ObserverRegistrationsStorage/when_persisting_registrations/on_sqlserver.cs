// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Observation.Registrations;
using Cratis.Chronicle.Storage.Sql.Sinks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Registrations.for_ObserverRegistrationsStorage.when_persisting_registrations;

[Collection(SqlServerCollection.Name)]
public class on_sqlserver(SqlServerFixture fixture) : ObserverRegistrationsStorageContract
{
    static readonly ServiceProvider _services = new ServiceCollection().BuildServiceProvider();
    string _connectionString;
    string _otherConnectionString;

    async Task Establish()
    {
        _connectionString = await fixture.CreateDatabase();
        await using var context = Context();
        await context.Database.MigrateAsync();
    }

    async Task Destroy()
    {
        await fixture.DropDatabase(_connectionString);
        if (_otherConnectionString is not null)
        {
            await fixture.DropDatabase(_otherConnectionString);
        }
    }

    protected override IObserverRegistrationsStorage Open() => new ObserverRegistrationsStorage(
        () => Task.FromResult(new DbContextScope<EventStoreDbContext>(Context(), () => { })), SerializerOptions);

    protected override async Task<IObserverRegistrationsStorage> OpenOtherStore()
    {
        _otherConnectionString = await fixture.CreateDatabase();
        await using var context = new EventStoreDbContext(Database.BuildOptions<EventStoreDbContext>(_services, _otherConnectionString));
        await context.Database.MigrateAsync();
        return new ObserverRegistrationsStorage(() => Task.FromResult(new DbContextScope<EventStoreDbContext>(new(Database.BuildOptions<EventStoreDbContext>(_services, _otherConnectionString)), () => { })), SerializerOptions);
    }

    EventStoreDbContext Context() => new(Database.BuildOptions<EventStoreDbContext>(_services, _connectionString));
}
