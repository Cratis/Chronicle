// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Aksio.MongoDB;
using MongoDB.Driver;
using StorageConfiguration = Aksio.Cratis.Kernel.Configuration.Storage;

namespace Aksio.Cratis.Kernel.Storage.MongoDB;

/// <summary>
/// Represents an implementation of <see cref="IDatabase"/>.
/// </summary>
public class Database : IDatabase
{
    readonly IMongoDatabase _database;
    readonly ConcurrentDictionary<EventStoreName, IEventStoreDatabase> _eventStoreDatabases = new();
    readonly ConcurrentDictionary<(EventStoreName, EventStoreNamespaceName), IMongoDatabase> _readModelDatabases = new();
    readonly IMongoDBClientManager _clientManager;
    readonly StorageConfiguration _configuration;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventStoreDatabase"/> class.
    /// </summary>
    /// <param name="clientManager"><see cref="IMongoDBClientFactory"/> for working with MongoDB.</param>
    /// <param name="configuration"><see cref="Storage"/> configuration.</param>
    public Database(
        IMongoDBClientManager clientManager,
        StorageConfiguration configuration)
    {
        var url = new MongoUrl(configuration.Cluster.ConnectionDetails.ToString());
        var settings = MongoClientSettings.FromUrl(url);
        var client = clientManager.GetClientFor(settings);
        _database = client.GetDatabase(url.DatabaseName);
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
    public IEventStoreDatabase GetEventStoreDatabase(EventStoreName eventStore)
    {
        if (_eventStoreDatabases.TryGetValue(eventStore, out var database))
        {
            return database;
        }

        return _eventStoreDatabases[eventStore] = new EventStoreDatabase(eventStore, _clientManager, _configuration);
    }

    /// <inheritdoc/>
    public IMongoDatabase GetReadModelDatabase(EventStoreName eventStore, EventStoreNamespaceName @namespace)
    {
        var key = (eventStore, @namespace);
        if (_readModelDatabases.TryGetValue(key, out var database))
        {
            return database;
        }

        var readModelsConfig = _configuration.Microservices.Get((string)eventStore).Tenants[@namespace].Get(WellKnownStorageTypes.ReadModels);
        var url = new MongoUrl(readModelsConfig.ConnectionDetails.ToString());
        var settings = MongoClientSettings.FromUrl(url);
        var client = _clientManager.GetClientFor(settings);
        database = client.GetDatabase(url.DatabaseName);
        _readModelDatabases[key] = database;
        return database;
    }
}
