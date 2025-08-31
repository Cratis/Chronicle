// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Chronicle.Concepts;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql;

/// <summary>
/// Represents an implementation of <see cref="IClusterStorage"/> for SQL.
/// </summary>
/// <param name="dbContext">The <see cref="ClusterDbContext"/> to use for storage operations.</param>
public class ClusterStorage(ClusterDbContext dbContext) : IClusterStorage
{
    /// <inheritdoc/>
    public async Task<IEnumerable<EventStoreName>> GetEventStores() =>
        await dbContext.EventStores.Select(es => (EventStoreName)es.Name).ToListAsync();

    /// <inheritdoc/>
    public ISubject<IEnumerable<EventStoreName>> ObserveEventStores() => throw new NotImplementedException();

    /// <inheritdoc/>
    public IEventStoreStorage CreateStorageForEventStore(EventStoreName eventStore, SinksFactory sinksFactory) =>
        new EventStoreStorage();

    /// <inheritdoc/>
    public Task SaveEventStore(EventStoreName eventStore)
    {
        dbContext.EventStores.Add(new EventStore { Name = eventStore });
        return dbContext.SaveChangesAsync();
    }
}
