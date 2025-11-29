// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Arc.MongoDB;
using Cratis.Chronicle.Concepts;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

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
    readonly IOptions<MongoDBOptions> _mongoDBOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventStoreDatabase"/> class.
    /// </summary>
    /// <param name="clientManager"><see cref="IMongoDBClientManager"/> for working with MongoDB.</param>
    /// <param name="mongoDBOptions"><see cref="Storage"/> configuration.</param>
    public Database(
        IMongoDBClientManager clientManager,
        IOptions<MongoDBOptions> mongoDBOptions)
    {
        var url = new MongoUrl(mongoDBOptions.Value.Server);
        var settings = MongoClientSettings.FromUrl(url);
        settings.DirectConnection = mongoDBOptions.Value.DirectConnection;
        var client = clientManager.GetClientFor(settings);
        _database = client.GetDatabase(WellKnownDatabaseNames.Chronicle);
        _clientManager = clientManager;
        _mongoDBOptions = mongoDBOptions;
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

        return _eventStoreDatabases[eventStore] = new EventStoreDatabase(eventStore, _clientManager, _mongoDBOptions);
    }

    /// <inheritdoc/>
    public IMongoDatabase GetReadModelDatabase(EventStoreName eventStore, EventStoreNamespaceName @namespace)
    {
        var key = (eventStore, @namespace);
        if (_readModelDatabases.TryGetValue(key, out var database))
        {
            return database;
        }

        // TODO: The name of the database should be configurable or coming from a configurable provider with conventions
        var databaseName = (@namespace == EventStoreNamespaceName.Default) ? $"{eventStore}" : $"{eventStore}+{@namespace}";
        var urlBuilder = new MongoUrlBuilder(_mongoDBOptions.Value.Server)
        {
            DatabaseName = databaseName,
            DirectConnection = _mongoDBOptions.Value.DirectConnection
        };

        var settings = MongoClientSettings.FromUrl(urlBuilder.ToMongoUrl());
        var client = _clientManager.GetClientFor(settings);
        database = client.GetDatabase(databaseName);
        _readModelDatabases[key] = database;
        return database;
    }
}
