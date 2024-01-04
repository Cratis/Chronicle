// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Kernel.Configuration;
using Aksio.MongoDB;
using MongoDB.Driver;

namespace Aksio.Cratis.MongoDB;

/// <summary>
/// Represents an implementation of <see cref="IEventStoreDatabase"/>.
/// </summary>
public class EventStoreDatabase : IEventStoreDatabase
{
    readonly IMongoDatabase _database;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventStoreDatabase"/> class.
    /// </summary>
    /// <param name="eventStore"><see cref="EventStore"/> the database is for.</param>
    /// <param name="clientFactory"><see cref="IMongoDBClientFactory"/> for working with MongoDB.</param>
    /// <param name="configuration"><see cref="Storage"/> configuration.</param>
    public EventStoreDatabase(
        EventStore eventStore,
        IMongoDBClientFactory clientFactory,
        Storage configuration)
    {
        var url = new MongoUrl(configuration.Microservices.Get((string)eventStore).Shared.Get(WellKnownStorageTypes.EventStore).ConnectionDetails.ToString());
        var client = clientFactory.Create(url);
        _database = client.GetDatabase(url.DatabaseName);
    }

    /// <inheritdoc/>
    public IMongoCollection<T> GetCollection<T>(string? collectionName = null)
    {
        if (collectionName == null)
        {
            return _database.GetCollection<T>();
        }

        return _database.GetCollection<T>(collectionName);
    }
}
