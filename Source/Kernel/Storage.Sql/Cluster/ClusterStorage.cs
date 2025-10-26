// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Sql.EventStores;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.Cluster;

/// <summary>
/// Represents an implementation of <see cref="IClusterStorage"/> for SQL.
/// </summary>
/// <param name="database">The <see cref="IDatabase"/> to use for storage operations.</param>
public class ClusterStorage(IDatabase database) : IClusterStorage
{
    /// <inheritdoc/>
    public async Task<IEnumerable<EventStoreName>> GetEventStores()
    {
        await using var scope = await database.Cluster();
        return await scope.DbContext.EventStores.Select(es => (EventStoreName)es.Name).ToListAsync();
    }

    /// <inheritdoc/>
    public ISubject<IEnumerable<EventStoreName>> ObserveEventStores() => throw new NotImplementedException();

    /// <inheritdoc/>
    public IEventStoreStorage CreateStorageForEventStore(EventStoreName eventStore, SinksFactory sinksFactory)
    {
        return new EventStoreStorage(eventStore, database);
    }

    /// <inheritdoc/>
    public async Task SaveEventStore(EventStoreName eventStore)
    {
        await using var scope = await database.Cluster();
        await scope.DbContext.EventStores.Upsert(new EventStore { Name = eventStore });
        await scope.DbContext.SaveChangesAsync();
    }
}
