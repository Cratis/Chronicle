// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Applications.MongoDB;
using MongoDB.Driver;
using StorageConfiguration = Cratis.Chronicle.Configuration.Storage;

namespace Cratis.Chronicle.Storage.MongoDB;

/// <summary>
/// Represents an implementation of <see cref="IEventStoreDatabase"/>.
/// </summary>
public class EventStoreDatabase : IEventStoreDatabase
{
    readonly IMongoDatabase _database;
    readonly ConcurrentDictionary<EventStoreNamespaceName, IEventStoreNamespaceDatabase> _eventStoreNamespaceDatabases = new();
    readonly EventStoreName _eventStore;
    readonly IMongoDBClientManager _clientManager;
    readonly StorageConfiguration _configuration;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventStoreDatabase"/> class.
    /// </summary>
    /// <param name="eventStore"><see cref="EventStoreName"/> the database is for.</param>
    /// <param name="clientManager"><see cref="IMongoDBClientFactory"/> for working with MongoDB.</param>
    /// <param name="configuration"><see cref="Storage"/> configuration.</param>
    public EventStoreDatabase(
        EventStoreName eventStore,
        IMongoDBClientManager clientManager,
        StorageConfiguration configuration)
    {
        var urlBuilder = new MongoUrlBuilder(configuration.ConnectionDetails.ToString())
        {
            DatabaseName = eventStore.Value
        };

        var settings = MongoClientSettings.FromUrl(urlBuilder.ToMongoUrl());
        var client = clientManager.GetClientFor(settings);

        _database = client.GetDatabase(eventStore.Value);
        _eventStore = eventStore;
        _clientManager = clientManager;
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
    public IEventStoreNamespaceDatabase GetNamespaceDatabase(EventStoreNamespaceName @namespace)
    {
        if (_eventStoreNamespaceDatabases.TryGetValue(@namespace, out var database))
        {
            return database;
        }

        return _eventStoreNamespaceDatabases[@namespace] = new EventStoreNamespaceDatabase(_eventStore, @namespace, _clientManager, _configuration);
    }
}
