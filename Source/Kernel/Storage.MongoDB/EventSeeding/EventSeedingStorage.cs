// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.EventSeeding;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.EventSeeding;

/// <summary>
/// Represents an implementation of <see cref="IEventSeedingStorage"/> for MongoDB.
/// </summary>
/// <param name="database">Provider for <see cref="IEventStoreNamespaceDatabase"/>.</param>
public class EventSeedingStorage(IEventStoreNamespaceDatabase database) : IEventSeedingStorage
{
    IMongoCollection<EventSeedingData> Collection => database.GetCollection<EventSeedingData>(WellKnownCollectionNames.EventSeeding);

    /// <inheritdoc/>
    public async Task<EventSeedingData> Get()
    {
        var filter = Builders<EventSeedingData>.Filter.Empty;
        using var cursor = await Collection.FindAsync(filter).ConfigureAwait(false);
        var result = cursor.FirstOrDefault();

        // Return result even if null, grain will handle initialization
        return result!;
    }

    /// <inheritdoc/>
    public async Task Save(EventSeedingData data)
    {
        var filter = Builders<EventSeedingData>.Filter.Empty;
        await Collection.ReplaceOneAsync(filter, data, new ReplaceOptions { IsUpsert = true });
    }
}
