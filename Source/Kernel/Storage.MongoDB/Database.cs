// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Arc.MongoDB;
using Cratis.Chronicle.Concepts;
using Cratis.Orleans.Storage.MongoDB.Serialization;
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
    readonly IOptions<MongoDBStorageOptions> _storageOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventStoreDatabase"/> class.
    /// </summary>
    /// <param name="clientManager"><see cref="IMongoDBClientManager"/> for working with MongoDB.</param>
    /// <param name="mongoDBOptions"><see cref="Storage"/> configuration.</param>
    /// <param name="customSerializers"><see cref="ICustomSerializers"/> for registering custom serializers.</param>
    /// <param name="storageOptions">Chronicle-specific database naming options.</param>
    public Database(
        IMongoDBClientManager clientManager,
        IOptions<MongoDBOptions> mongoDBOptions,
        ICustomSerializers customSerializers,
        IOptions<MongoDBStorageOptions>? storageOptions = null)
    {
        customSerializers.Register();

        var url = new MongoUrl(mongoDBOptions.Value.Server);
        var settings = MongoClientSettings.FromUrl(url);
        if (mongoDBOptions.Value.DirectConnection == true)
        {
            settings.DirectConnection = true;
        }
        var client = clientManager.GetClientFor(settings);
        _storageOptions = storageOptions ?? Options.Create(new MongoDBStorageOptions());
        _database = client.GetDatabase(DatabaseNames.WithPrefix(WellKnownDatabaseNames.Chronicle, _storageOptions.Value.DatabaseNamePrefix));
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

        return _eventStoreDatabases[eventStore] = new EventStoreDatabase(eventStore, _clientManager, _mongoDBOptions, _storageOptions);
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
        var databaseName = DatabaseNames.ForReadModels(eventStore, @namespace, _storageOptions.Value.DatabaseNamePrefix);
        var urlBuilder = new MongoUrlBuilder(_mongoDBOptions.Value.Server);
        // Preserve the URI path's implicit authentication database before selecting Chronicle's database.
        urlBuilder.AuthenticationSource ??= urlBuilder.DatabaseName;
        urlBuilder.DatabaseName = databaseName;
        if (_mongoDBOptions.Value.DirectConnection == true)
        {
            urlBuilder.DirectConnection = true;
        }

        var settings = MongoClientSettings.FromUrl(urlBuilder.ToMongoUrl());
        var client = _clientManager.GetClientFor(settings);
        database = client.GetDatabase(databaseName);
        _readModelDatabases[key] = database;
        return database;
    }
}
