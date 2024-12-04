// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Applications.MongoDB;
using Cratis.Chronicle.Concepts;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

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
    readonly IOptions<MongoDBOptions> _mongoDBOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventStoreDatabase"/> class.
    /// </summary>
    /// <param name="eventStore"><see cref="EventStoreName"/> the database is for.</param>
    /// <param name="clientManager"><see cref="IMongoDBClientFactory"/> for working with MongoDB.</param>
    /// <param name="mongoDBOptions"><see cref="Storage"/> configuration.</param>
    public EventStoreDatabase(
        EventStoreName eventStore,
        IMongoDBClientManager clientManager,
        IOptions<MongoDBOptions> mongoDBOptions)
    {
        var urlBuilder = new MongoUrlBuilder(mongoDBOptions.Value.Server)
        {
            DatabaseName = eventStore.Value,
            DirectConnection = mongoDBOptions.Value.DirectConnection
        };

        var settings = MongoClientSettings.FromUrl(urlBuilder.ToMongoUrl());
        var client = clientManager.GetClientFor(settings);

        // TODO: The name of the database should be configurable or coming from a configurable provider with conventions
        _database = client.GetDatabase($"{eventStore.Value}+es");
        _eventStore = eventStore;
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
    public IEventStoreNamespaceDatabase GetNamespaceDatabase(EventStoreNamespaceName @namespace)
    {
        if (_eventStoreNamespaceDatabases.TryGetValue(@namespace, out var database))
        {
            return database;
        }

        return _eventStoreNamespaceDatabases[@namespace] = new EventStoreNamespaceDatabase(_eventStore, @namespace, _clientManager, _mongoDBOptions);
    }
}
