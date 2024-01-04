// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.EventSequences;
using Aksio.Cratis.Kernel.Storage.Observation;
using Aksio.MongoDB;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Aksio.Cratis.Kernel.MongoDB;

/// <summary>
/// Represents an implementation of <see cref="IEventStoreInstanceDatabase"/>.
/// </summary>
[SingletonPerMicroserviceAndTenant]
public class EventStoreInstanceDatabase : IEventStoreInstanceDatabase
{
    readonly IMongoDatabase _database;
    readonly HashSet<EventSequenceId> _indexedEventSequences = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="EventStoreInstanceDatabase"/> class.
    /// </summary>
    /// <param name="executionContext"><see cref="ExecutionContext"/> the database is for.</param>
    /// <param name="mongoDBClientFactory"><see cref="IMongoDBClientFactory"/> for creating clients.</param>
    /// <param name="configuration"><see cref="Configuration.Storage"/> configuration.</param>
    public EventStoreInstanceDatabase(
        ExecutionContext executionContext,
        IMongoDBClientFactory mongoDBClientFactory,
        Configuration.Storage configuration)
    {
        var storageTypes = configuration.Microservices
                                .Get(executionContext.MicroserviceId).Tenants
                                .Get(executionContext.TenantId);
        var eventStoreForTenant = storageTypes.Get(WellKnownStorageTypes.EventStore);
        var url = new MongoUrl(eventStoreForTenant.ConnectionDetails.ToString());
        var settings = MongoClientSettings.FromUrl(url);

        // settings.ReadPreference = ReadPreference.SecondaryPreferred;
        var client = mongoDBClientFactory.Create(settings);
        _database = client.GetDatabase(url.DatabaseName);
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
        if (!eventSequenceId.IsEventLog)
        {
            if (eventSequenceId == EventSequenceId.SystemId)
            {
                collectionName = WellKnownCollectionNames.System;
            }
            else if (eventSequenceId.IsOutbox)
            {
                collectionName = WellKnownCollectionNames.Outbox;
            }
            else
            {
                collectionName = WellKnownCollectionNames.Inbox;
            }
        }

        return collectionName;
    }
}
