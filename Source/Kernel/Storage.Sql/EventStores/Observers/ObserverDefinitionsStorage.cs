// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.EventTypes;
using Cratis.Chronicle.Storage.Observation;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Observers;

/// <summary>
/// Represents an implementation of <see cref="IEventTypesStorage"/> for SQL.
/// </summary>
/// <param name="eventStore">The name of the event store.</param>
/// <param name="database">The <see cref="IDatabase"/> to use for storage operations.</param>
public class ObserverDefinitionsStorage(EventStoreName eventStore, IDatabase database) : IObserverDefinitionsStorage
{
    /// <inheritdoc/>
    public async Task<IEnumerable<Observation.ObserverDefinition>> GetAll()
    {
        await using var scope = await database.EventStore(eventStore);
        var observers = await scope.DbContext.Observers.ToListAsync();
        return observers.Select(observer => observer.ToKernel()).ToArray();
    }

    /// <inheritdoc/>
    public async Task<bool> Has(ObserverId id)
    {
        await using var scope = await database.EventStore(eventStore);
        return await scope.DbContext.Observers.AnyAsync(observer => observer.Id == id);
    }

    /// <inheritdoc/>
    public async Task<Observation.ObserverDefinition> Get(ObserverId id)
    {
        await using var scope = await database.EventStore(eventStore);
        return await scope.DbContext.Observers
            .Where(observer => observer.Id == id)
            .Select(observer => observer.ToKernel())
            .FirstOrDefaultAsync() ?? Observation.ObserverDefinition.Empty;
    }

    /// <inheritdoc/>
    public async Task Delete(ObserverId id)
    {
        await using var scope = await database.EventStore(eventStore);
        var observer = await scope.DbContext.Observers.FindAsync(id);
        if (observer != null)
        {
            scope.DbContext.Observers.Remove(observer);
            await scope.DbContext.SaveChangesAsync();
        }
    }

    /// <inheritdoc/>
    public async Task Save(Observation.ObserverDefinition definition)
    {
        await using var scope = await database.EventStore(eventStore);
        var entity = definition.ToSql();
        await scope.DbContext.Observers.Upsert(entity);
        await scope.DbContext.SaveChangesAsync();
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Observation.ObserverDefinition>> GetForEventTypes(IEnumerable<EventType> eventTypes)
    {
        var eventTypeIds = eventTypes.Select(et => et.Id).ToHashSet();
        await using var scope = await database.EventStore(eventStore);
        return await scope.DbContext.Observers
            .Where(observer => observer.EventTypes.Any(et => eventTypeIds.Contains(et.EventType)))
            .Select(observer => observer.ToKernel())
            .ToListAsync();
    }
}
