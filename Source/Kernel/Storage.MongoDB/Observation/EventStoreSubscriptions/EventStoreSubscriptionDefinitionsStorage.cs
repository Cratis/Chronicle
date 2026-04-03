// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.EventStoreSubscriptions;
using Cratis.Chronicle.Storage.Observation.EventStoreSubscriptions;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Observation.EventStoreSubscriptions;

/// <summary>
/// Represents a <see cref="IEventStoreSubscriptionDefinitionsStorage"/> for event store subscription definitions in MongoDB.
/// </summary>
/// <param name="eventStoreDatabase">The <see cref="IEventStoreDatabase"/>.</param>
public class EventStoreSubscriptionDefinitionsStorage(
    IEventStoreDatabase eventStoreDatabase) : IEventStoreSubscriptionDefinitionsStorage
{
    IMongoCollection<EventStoreSubscriptionDefinition> Collection =>
        eventStoreDatabase.GetCollection<EventStoreSubscriptionDefinition>(WellKnownCollectionNames.EventStoreSubscriptionDefinitions);

    /// <inheritdoc/>
    public async Task<IEnumerable<Concepts.Observation.EventStoreSubscriptions.EventStoreSubscriptionDefinition>> GetAll()
    {
        using var result = await Collection.FindAsync(FilterDefinition<EventStoreSubscriptionDefinition>.Empty);
        return (await result.ToListAsync()).Select(definition => definition.ToKernel()).ToArray();
    }

    /// <inheritdoc/>
    public Task<bool> Has(EventStoreSubscriptionId id) =>
        Collection.Find(r => r.Id == id).AnyAsync();

    /// <inheritdoc/>
    public async Task<Concepts.Observation.EventStoreSubscriptions.EventStoreSubscriptionDefinition?> Get(EventStoreSubscriptionId id)
    {
        using var result = await Collection.FindAsync(definition => definition.Id == id);
        var found = await result.FirstOrDefaultAsync();
        return found?.ToKernel();
    }

    /// <inheritdoc/>
    public Task Delete(EventStoreSubscriptionId id) =>
        Collection.DeleteOneAsync(definition => definition.Id == id);

    /// <inheritdoc/>
    public Task Save(Concepts.Observation.EventStoreSubscriptions.EventStoreSubscriptionDefinition definition) =>
        Collection.ReplaceOneAsync(
            filter: def => def.Id == definition.Identifier,
            replacement: definition.ToMongoDB(),
            options: new ReplaceOptions { IsUpsert = true });
}
