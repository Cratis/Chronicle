// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Observation;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Observation;

/// <summary>
/// Represents a MongoDB implementation of <see cref="IObserverDefinitionsStorage"/>.
/// </summary>
/// <param name="eventStoreDatabase"><see cref="IEventStoreDatabase"/> instance.</param>
public class ObserverDefinitionsStorage(IEventStoreDatabase eventStoreDatabase) : IObserverDefinitionsStorage
{
    IMongoCollection<ObserverDefinition> _collection => eventStoreDatabase.GetCollection<ObserverDefinition>(WellKnownCollectionNames.Observers);

    /// <inheritdoc/>
    public async Task<IEnumerable<Chronicle.Storage.Observation.ObserverDefinition>> GetAll()
    {
        using var result = await _collection.FindAsync(FilterDefinition<ObserverDefinition>.Empty);
        var definitions = result.ToList();
        return definitions.Select(definition => definition.ToKernel()).ToArray();
    }

    /// <inheritdoc/>
    public Task<bool> Has(ObserverId id) =>
        _collection.Find(r => r.Id == id).AnyAsync();

    /// <inheritdoc/>
    public async Task<Chronicle.Storage.Observation.ObserverDefinition> Get(ObserverId id)
    {
        using var result = await _collection.FindAsync(definition => definition.Id == id);
        return result.Single().ToKernel();
    }

    /// <inheritdoc/>
    public Task Delete(ObserverId id) =>
        _collection.DeleteOneAsync(definition => definition.Id == id);

    /// <inheritdoc/>
    public Task Save(Chronicle.Storage.Observation.ObserverDefinition definition) =>
        _collection.ReplaceOneAsync(
            filter: def => def.Id == definition.Identifier,
            replacement: definition.ToMongoDB(),
            options: new ReplaceOptions { IsUpsert = true });

    /// <inheritdoc/>
    public async Task<IEnumerable<Chronicle.Storage.Observation.ObserverDefinition>> GetForEventTypes(IEnumerable<EventType> eventTypes)
    {
        var eventTypeIds = eventTypes.Select(_ => _.Id.ToString()).ToArray();
        var result = await _collection
            .FindAsync(_ => _.EventTypes.Any(_ => eventTypeIds.Contains(_)));
        return result.ToList().ToKernel();
    }
}
