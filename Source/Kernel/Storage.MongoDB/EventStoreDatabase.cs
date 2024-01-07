// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Aksio.MongoDB;
using MongoDB.Driver;
using StorageConfiguration = Aksio.Cratis.Kernel.Configuration.Storage;

namespace Aksio.Cratis.Kernel.Storage.MongoDB;

/// <summary>
/// Represents an implementation of <see cref="IEventStoreDatabase"/>.
/// </summary>
public class EventStoreDatabase : IEventStoreDatabase
{
    readonly IMongoDatabase _database;
    readonly ConcurrentDictionary<EventStoreNamespace, IEventStoreNamespaceDatabase> _eventStoreNamespaceDatabases = new();
    readonly EventStore _eventStore;
    readonly IMongoDBClientFactory _clientFactory;
    readonly StorageConfiguration _configuration;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventStoreDatabase"/> class.
    /// </summary>
    /// <param name="eventStore"><see cref="EventStore"/> the database is for.</param>
    /// <param name="clientFactory"><see cref="IMongoDBClientFactory"/> for working with MongoDB.</param>
    /// <param name="configuration"><see cref="Storage"/> configuration.</param>
    public EventStoreDatabase(
        EventStore eventStore,
        IMongoDBClientFactory clientFactory,
        StorageConfiguration configuration)
    {
        var url = new MongoUrl(configuration.Microservices.Get((string)eventStore).Shared.Get(WellKnownStorageTypes.EventStore).ConnectionDetails.ToString());
        var client = clientFactory.Create(url);
        _database = client.GetDatabase(url.DatabaseName);
        _eventStore = eventStore;
        _clientFactory = clientFactory;
        _configuration = configuration;
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

    /// <inheritdoc/>
    public IEventStoreNamespaceDatabase GetNamespaceDatabase(EventStoreNamespace @namespace)
    {
        if (_eventStoreNamespaceDatabases.TryGetValue(@namespace, out var database))
        {
            return database;
        }

        return _eventStoreNamespaceDatabases[@namespace] = new EventStoreNamespaceDatabase(_eventStore, @namespace, _clientFactory, _configuration);
    }
}
