// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Chronicle.Concepts;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.Cluster;

/// <summary>
/// Represents an implementation of <see cref="IClusterStorage"/> for SQL.
/// </summary>
/// <param name="serviceProvider">The service provider.</param>
/// <param name="dbContext">The <see cref="ClusterDbContext"/> to use for storage operations.</param>
public class ClusterStorage(IServiceProvider serviceProvider, ClusterDbContext dbContext) : IClusterStorage
{
    /// <inheritdoc/>
    public async Task<IEnumerable<EventStoreName>> GetEventStores() =>
        await dbContext.EventStores.Select(es => (EventStoreName)es.Name).ToListAsync();

    /// <inheritdoc/>
    public ISubject<IEnumerable<EventStoreName>> ObserveEventStores() => throw new NotImplementedException();

    /// <inheritdoc/>
    public IEventStoreStorage CreateStorageForEventStore(EventStoreName eventStore, SinksFactory sinksFactory) =>
        new EventStoreStorage(serviceProvider, eventStore);

    /// <inheritdoc/>
    public Task SaveEventStore(EventStoreName eventStore)
    {
        dbContext.EventStores.Add(new EventStore { Name = eventStore });
        return dbContext.SaveChangesAsync();
    }
}
