// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.MongoDB;
using MongoDB.Driver;
using StorageConfiguration = Cratis.Chronicle.Configuration.Storage;

namespace Cratis.Chronicle.Storage.MongoDB;

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
        var url = new MongoUrl(configuration.ConnectionDetails.ToString());
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

        // TODO: This should be a configurable convention.
        var databaseName = $"{eventStore}+{@namespace}-rm";
        var urlBuilder = new MongoUrlBuilder(_configuration.ConnectionDetails.ToString())
        {
            DatabaseName = databaseName
        };

        var settings = MongoClientSettings.FromUrl(urlBuilder.ToMongoUrl());
        var client = _clientManager.GetClientFor(settings);
        database = client.GetDatabase(databaseName);
        _readModelDatabases[key] = database;
        return database;
    }
}
