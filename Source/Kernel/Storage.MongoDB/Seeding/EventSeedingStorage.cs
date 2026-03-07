// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Seeding;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Seeding;

/// <summary>
/// Represents an implementation of <see cref="IEventSeedingStorage"/> for MongoDB.
/// </summary>
public class EventSeedingStorage : IEventSeedingStorage
{
    readonly IMongoCollection<EventSeeds> _collection;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventSeedingStorage"/> class for namespace-level storage.
    /// </summary>
    /// <param name="database">Provider for <see cref="IEventStoreNamespaceDatabase"/>.</param>
    public EventSeedingStorage(IEventStoreNamespaceDatabase database)
    {
        _collection = database.GetCollection<EventSeeds>(WellKnownCollectionNames.EventSeeds);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EventSeedingStorage"/> class for event store-level storage.
    /// </summary>
    /// <param name="database">Provider for <see cref="IEventStoreDatabase"/>.</param>
    public EventSeedingStorage(IEventStoreDatabase database)
    {
        _collection = database.GetCollection<EventSeeds>(WellKnownCollectionNames.EventSeeds);
    }

    /// <inheritdoc/>
    public async Task<EventSeeds> Get()
    {
        var filter = CreateFilter();
        using var cursor = await _collection.FindAsync(filter).ConfigureAwait(false);
        return cursor.FirstOrDefault();
    }

    /// <inheritdoc/>
    public async Task Save(EventSeeds data)
    {
        var filter = CreateFilter();
        await _collection.ReplaceOneAsync(filter, data, new ReplaceOptions { IsUpsert = true });
    }

    static FilterDefinition<EventSeeds> CreateFilter() => Builders<EventSeeds>.Filter.Eq(new StringFieldDefinition<EventSeeds, int>("_id"), 0);
}
