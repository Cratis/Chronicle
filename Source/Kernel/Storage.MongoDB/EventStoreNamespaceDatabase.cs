// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Applications.MongoDB;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.Observation;
using Cratis.Chronicle.Storage.MongoDB.Observation;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB;

/// <summary>
/// Represents an implementation of <see cref="IEventStoreNamespaceDatabase"/>.
/// </summary>
public class EventStoreNamespaceDatabase : IEventStoreNamespaceDatabase
{
    const string ClosedStreamCollectionName = "closed-streams";

    readonly IMongoDatabase _database;
    readonly ConcurrentDictionary<EventSequenceId, bool> _indexedEventSequences = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="EventStoreNamespaceDatabase"/> class.
    /// </summary>
    /// <param name="eventStore"><see cref="EventStoreName"/> the database is for.</param>
    /// <param name="namespace"><see cref="EventStoreNamespaceName"/> the database is for.</param>
    /// <param name="clientManager"><see cref="IMongoDBClientFactory"/> for creating clients.</param>
    /// <param name="mongoDBOptions"><see cref="Configuration.Storage"/> configuration.</param>
    public EventStoreNamespaceDatabase(
        EventStoreName eventStore,
        EventStoreNamespaceName @namespace,
        IMongoDBClientManager clientManager,
        IOptions<MongoDBOptions> mongoDBOptions)
    {
        var databaseName = $"{eventStore}+es+{@namespace}";
        var urlBuilder = new MongoUrlBuilder(mongoDBOptions.Value.Server)
        {
            DatabaseName = databaseName,
            DirectConnection = mongoDBOptions.Value.DirectConnection
        };
        var settings = MongoClientSettings.FromUrl(urlBuilder.ToMongoUrl());

        // TODO: Performance optimization - separate reads from writes in a clustered setup. Read from secondary.
        // settings.ReadPreference = ReadPreference.SecondaryPreferred;
        var client = clientManager.GetClientFor(settings);
        _database = client.GetDatabase(databaseName);
    }

    /// <inheritdoc/>
    public IMongoCollection<T> GetCollection<T>(string? name = null) => name == null ? _database.GetCollection<T>() : _database.GetCollection<T>(name);

    /// <inheritdoc/>
    public IMongoCollection<Event> GetEventSequenceCollectionFor(EventSequenceId eventSequenceId)
    {
        var collectionName = GetEventSequenceCollectionNameFor(eventSequenceId);
        var collection = _database.GetCollection<Event>(collectionName);
        CreateIndexesForEventSequenceIfNotCreated(collection, eventSequenceId);
        return collection;
    }

    /// <inheritdoc/>
    public IMongoCollection<ClosedStreamState> GetClosedStreamCollectionFor(EventSequenceId eventSequenceId)
    {
        var collectionName = GetClosedStreamCollectionNameFor(eventSequenceId);
        return _database.GetCollection<ClosedStreamState>(collectionName);
    }

    /// <inheritdoc/>
    public IMongoCollection<BsonDocument> GetEventSequenceCollectionAsBsonFor(EventSequenceId eventSequenceId)
    {
        var collectionName = GetEventSequenceCollectionNameFor(eventSequenceId);
        return _database.GetCollection<BsonDocument>(collectionName);
    }

    /// <inheritdoc/>
    public IMongoCollection<ObserverState> GetObserverStateCollection() => GetCollection<ObserverState>(WellKnownCollectionNames.Observers);

    void CreateIndexesForEventSequenceIfNotCreated(IMongoCollection<Event> collection, EventSequenceId eventSequenceId)
    {
        if (!_indexedEventSequences.ContainsKey(eventSequenceId))
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

            collection.Indexes.CreateOne(
                new CreateIndexModel<Event>(
                    Builders<Event>.IndexKeys.Ascending(_ => _.EventSourceType),
                    new CreateIndexOptions
                    {
                        Name = "eventSourceType",
                        Background = true
                    }));

            collection.Indexes.CreateOne(
                new CreateIndexModel<Event>(
                    Builders<Event>.IndexKeys.Ascending(_ => _.EventStreamType),
                    new CreateIndexOptions
                    {
                        Name = "eventStreamType",
                        Background = true
                    }));

            collection.Indexes.CreateOne(
                new CreateIndexModel<Event>(
                    Builders<Event>.IndexKeys.Ascending(_ => _.EventStreamId),
                    new CreateIndexOptions
                    {
                        Name = "eventStreamId",
                        Background = true
                    }));

            collection.Indexes.CreateOne(
                new CreateIndexModel<Event>(
                    Builders<Event>.IndexKeys.Combine(
                        Builders<Event>.IndexKeys.Ascending(_ => _.EventStreamType),
                        Builders<Event>.IndexKeys.Ascending(_ => _.EventStreamId)),
                    new CreateIndexOptions
                    {
                        Name = "eventStreamType_eventStreamId",
                        Background = true
                    }));

            collection.Indexes.CreateOne(
                new CreateIndexModel<Event>(
                    Builders<Event>.IndexKeys.Combine(
                        Builders<Event>.IndexKeys.Ascending(_ => _.EventStreamType),
                        Builders<Event>.IndexKeys.Ascending(_ => _.EventStreamId),
                        Builders<Event>.IndexKeys.Ascending(_ => _.EventSourceType)),
                    new CreateIndexOptions
                    {
                        Name = "eventStreamType_eventStreamId_eventSourceType",
                        Background = true
                    }));

            collection.Indexes.CreateOne(
                new CreateIndexModel<Event>(
                    Builders<Event>.IndexKeys.Combine(
                        Builders<Event>.IndexKeys.Ascending(_ => _.EventSourceId),
                        Builders<Event>.IndexKeys.Ascending(_ => _.EventStreamType),
                        Builders<Event>.IndexKeys.Ascending(_ => _.EventStreamId)),
                    new CreateIndexOptions
                    {
                        Name = "eventSourceId_eventStreamType_eventStreamId",
                        Background = true
                    }));

            collection.Indexes.CreateOne(
                new CreateIndexModel<Event>(
                    Builders<Event>.IndexKeys.Combine(
                        Builders<Event>.IndexKeys.Ascending(_ => _.EventSourceId),
                        Builders<Event>.IndexKeys.Ascending(_ => _.EventStreamType),
                        Builders<Event>.IndexKeys.Ascending(_ => _.EventStreamId),
                        Builders<Event>.IndexKeys.Ascending(_ => _.EventSourceType)),
                    new CreateIndexOptions
                    {
                        Name = "eventSourceId_eventStreamType_eventStreamId_eventSourceType",
                        Background = true
                    }));

            collection.Indexes.CreateOne(
                new CreateIndexModel<Event>(
                    Builders<Event>.IndexKeys.Combine(
                        Builders<Event>.IndexKeys.Ascending(_ => _.EventSourceId),
                        Builders<Event>.IndexKeys.Ascending(_ => _.Type),
                        Builders<Event>.IndexKeys.Ascending(_ => _.EventStreamType),
                        Builders<Event>.IndexKeys.Ascending(_ => _.EventStreamId)),
                    new CreateIndexOptions
                    {
                        Name = "eventSourceId_eventTypeId_eventStreamType_eventStreamId",
                        Background = true
                    }));

            collection.Indexes.CreateOne(
                new CreateIndexModel<Event>(
                    Builders<Event>.IndexKeys.Combine(
                        Builders<Event>.IndexKeys.Ascending(_ => _.EventSourceId),
                        Builders<Event>.IndexKeys.Ascending(_ => _.Type),
                        Builders<Event>.IndexKeys.Ascending(_ => _.EventStreamType),
                        Builders<Event>.IndexKeys.Ascending(_ => _.EventStreamId),
                        Builders<Event>.IndexKeys.Ascending(_ => _.EventSourceType)),
                    new CreateIndexOptions
                    {
                        Name = "eventSourceId_eventTypeId_eventStreamType_eventStreamId_eventSourceType",
                        Background = true
                    }));

            _indexedEventSequences.TryAdd(eventSequenceId, true);
        }
    }

    string GetEventSequenceCollectionNameFor(EventSequenceId eventSequenceId)
    {
        var collectionName = WellKnownCollectionNames.EventLog;
        if (!eventSequenceId.IsEventLog && eventSequenceId == EventSequenceId.System)
        {
            collectionName = WellKnownCollectionNames.System;
        }

        return collectionName;
    }

    string GetClosedStreamCollectionNameFor(EventSequenceId eventSequenceId) =>
        GetEventSequenceCollectionNameFor(eventSequenceId) + ClosedStreamCollectionName;
}
