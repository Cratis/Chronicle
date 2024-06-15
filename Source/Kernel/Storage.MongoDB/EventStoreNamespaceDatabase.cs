// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Observation;
using Cratis.EventSequences;
using Cratis.MongoDB;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB;

/// <summary>
/// Represents an implementation of <see cref="IEventStoreNamespaceDatabase"/>.
/// </summary>
public class EventStoreNamespaceDatabase : IEventStoreNamespaceDatabase
{
    readonly IMongoDatabase _database;
    readonly HashSet<EventSequenceId> _indexedEventSequences = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="EventStoreNamespaceDatabase"/> class.
    /// </summary>
    /// <param name="eventStore"><see cref="EventStoreName"/> the database is for.</param>
    /// <param name="namespace"><see cref="EventStoreNamespaceName"/> the database is for.</param>
    /// <param name="clientManager"><see cref="IMongoDBClientFactory"/> for creating clients.</param>
    /// <param name="configuration"><see cref="Configuration.Storage"/> configuration.</param>
    public EventStoreNamespaceDatabase(
        EventStoreName eventStore,
        EventStoreNamespaceName @namespace,
        IMongoDBClientManager clientManager,
        Configuration.Storage configuration)
    {
        var databaseName = $"{eventStore}-{@namespace}";

        var urlBuilder = new MongoUrlBuilder(configuration.ConnectionDetails.ToString())
        {
            DatabaseName = databaseName
        };

        var settings = MongoClientSettings.FromUrl(urlBuilder.ToMongoUrl());

        // settings.ReadPreference = ReadPreference.SecondaryPreferred;
        var client = clientManager.GetClientFor(settings);
        _database = client.GetDatabase(databaseName);
    }

    /// <inheritdoc/>
    public IMongoCollection<T> GetCollection<T>(string? name = null) => name == null ? _database.GetCollection<T>() : _database.GetCollection<T>(name);

    /// <inheritdoc/>
    public IMongoCollection<Event> GetEventSequenceCollectionFor(EventSequenceId eventSequenceId)
    {
        var collectionName = GetCollectionNameFor(eventSequenceId);
        var collection = _database.GetCollection<Event>(collectionName);
        CreateIndexesForEventSequenceIfNotCreated(collection, eventSequenceId);
        return collection;
    }

    /// <inheritdoc/>
    public IMongoCollection<BsonDocument> GetEventSequenceCollectionAsBsonFor(EventSequenceId eventSequenceId)
    {
        var collectionName = GetCollectionNameFor(eventSequenceId);
        return _database.GetCollection<BsonDocument>(collectionName);
    }

    /// <inheritdoc/>
    public IMongoCollection<ObserverState> GetObserverStateCollection() => GetCollection<ObserverState>(WellKnownCollectionNames.Observers);

    void CreateIndexesForEventSequenceIfNotCreated(IMongoCollection<Event> collection, EventSequenceId eventSequenceId)
    {
        if (!_indexedEventSequences.Contains(eventSequenceId))
        {
            collection.Indexes.CreateOne(
                new CreateIndexModel<Event>(
                    Builders<Event>.IndexKeys.Text(_ => _.EventSourceId),
                    new CreateIndexOptions
                    {
                        Name = "eventSourceId",
                        Background = true
                    }));

            collection.Indexes.CreateOne(
                new CreateIndexModel<Event>(
                    Builders<Event>.IndexKeys.Ascending(_ => _.Type),
                    new CreateIndexOptions
                    {
                        Name = "eventTypeId",
                        Background = true
                    }));

            collection.Indexes.CreateOne(
                new CreateIndexModel<Event>(
                    Builders<Event>.IndexKeys.Ascending(_ => _.Occurred),
                    new CreateIndexOptions
                    {
                        Name = "occurred",
                        Background = true
                    }));

            collection.Indexes.CreateOne(
                new CreateIndexModel<Event>(
                    Builders<Event>.IndexKeys.Combine(
                        Builders<Event>.IndexKeys.Ascending(_ => _.EventSourceId),
                        Builders<Event>.IndexKeys.Ascending(_ => _.Type)),
                    new CreateIndexOptions
                    {
                        Name = "eventSourceId_eventTypeId",
                        Background = true
                    }));

            _indexedEventSequences.Add(eventSequenceId);
        }
    }

    string GetCollectionNameFor(EventSequenceId eventSequenceId)
    {
        var collectionName = WellKnownCollectionNames.EventLog;
        if (!eventSequenceId.IsEventLog && eventSequenceId == EventSequenceId.SystemId)
        {
            collectionName = WellKnownCollectionNames.System;
        }

        return collectionName;
    }
}
