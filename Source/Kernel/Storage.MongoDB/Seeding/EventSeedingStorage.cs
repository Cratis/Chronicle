// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Seeding;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Seeding;

/// <summary>
/// Represents an implementation of <see cref="IEventSeedingStorage"/> for MongoDB.
/// </summary>
/// <param name="database">Provider for <see cref="IEventStoreNamespaceDatabase"/>.</param>
public class EventSeedingStorage(IEventStoreNamespaceDatabase database) : IEventSeedingStorage
{
    IMongoCollection<EventSeeds> Collection => database.GetCollection<EventSeeds>(WellKnownCollectionNames.EventSeeding);

    /// <inheritdoc/>
    public async Task<EventSeeds> Get()
    {
        var filter = Builders<EventSeeds>.Filter.Empty;
        using var cursor = await Collection.FindAsync(filter).ConfigureAwait(false);
        return cursor.FirstOrDefault();
    }

    /// <inheritdoc/>
    public async Task Save(EventSeeds data)
    {
        var filter = Builders<EventSeeds>.Filter.Empty;
        await Collection.ReplaceOneAsync(filter, data, new ReplaceOptions { IsUpsert = true });
    }
}
