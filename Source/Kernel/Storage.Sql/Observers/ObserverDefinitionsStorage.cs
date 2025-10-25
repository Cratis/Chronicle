// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.EventTypes;
using Cratis.Chronicle.Storage.Observation;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.Observers;

/// <summary>
/// Represents an implementation of <see cref="IEventTypesStorage"/> for SQL.
/// </summary>
/// <param name="dbContext">The database context.</param>
public class ObserverDefinitionsStorage(EventStoreDbContext dbContext) : IObserverDefinitionsStorage
{
    /// <inheritdoc/>
    public async Task<IEnumerable<Observation.ObserverDefinition>> GetAll()
    {
        var observers = await dbContext.Observers.ToListAsync();
        return observers.Select(observer => observer.ToKernel()).ToArray();
    }

    /// <inheritdoc/>
    public Task<bool> Has(ObserverId id) =>
        dbContext.Observers.AnyAsync(observer => observer.Id == id);

    /// <inheritdoc/>
    public Task<Observation.ObserverDefinition> Get(ObserverId id) =>
        dbContext.Observers
            .Where(observer => observer.Id == id)
            .Select(observer => observer.ToKernel())
            .FirstOrDefaultAsync()!;

    /// <inheritdoc/>
    public async Task Delete(ObserverId id)
    {
        var observer = await dbContext.Observers.FindAsync(id);
        if (observer != null)
        {
            dbContext.Observers.Remove(observer);
            await dbContext.SaveChangesAsync();
        }
    }

    /// <inheritdoc/>
    public async Task Save(Observation.ObserverDefinition definition)
    {
        var entity = definition.ToSql();
        dbContext.Observers.Add(entity);
        await dbContext.SaveChangesAsync();
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Observation.ObserverDefinition>> GetForEventTypes(IEnumerable<EventType> eventTypes)
    {
        var eventTypeIds = eventTypes.Select(et => et.Id).ToHashSet();
        return await dbContext.Observers
            .Where(observer => observer.EventTypes.Any(et => eventTypeIds.Contains(et.EventType.Id)))
            .Select(observer => observer.ToKernel())
            .ToListAsync();
    }
}
