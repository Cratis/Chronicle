// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Observation.Registrations;
using Cratis.Chronicle.Storage.Sql.Sinks;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Registrations.for_ObserverRegistrationsStorage.when_persisting_registrations;

[Collection(PostgreSqlCollection.Name)]
public class on_postgresql(PostgreSqlFixture fixture) : ObserverRegistrationsStorageContract
{
    static readonly ServiceProvider _services = new ServiceCollection().BuildServiceProvider();
    string _connectionString;
    string _otherConnectionString;

    async Task Establish() => _connectionString = await fixture.CreateMigratedDatabase<EventStoreDbContext>();

    async Task Destroy()
    {
        await fixture.DropDatabase(_connectionString);
        if (_otherConnectionString is not null)
        {
            await fixture.DropDatabase(_otherConnectionString);
        }
    }

    protected override async Task<IObserverRegistrationsStorage> OpenOtherStore()
    {
        _otherConnectionString = await fixture.CreateMigratedDatabase<EventStoreDbContext>();
        return new ObserverRegistrationsStorage(() => Task.FromResult(new DbContextScope<EventStoreDbContext>(new(Database.BuildOptions<EventStoreDbContext>(_services, _otherConnectionString)), () => { })), SerializerOptions);
    }

    protected override IObserverRegistrationsStorage Open() => new ObserverRegistrationsStorage(
        () => Task.FromResult(new DbContextScope<EventStoreDbContext>(new(Database.BuildOptions<EventStoreDbContext>(_services, _connectionString)), () => { })), SerializerOptions);
}
