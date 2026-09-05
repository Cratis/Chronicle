// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Arc.MongoDB;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Storage.MongoDB.EventSequences.Mutations;
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
    /// <summary>
    /// Indexes dropped as part of the index prune: the "eventSourceId" text index (no $text query exists; equality
    /// is served by the eventSourceId compounds), the single-field "eventTypeId" ({type:1}, covered by the
    /// type_sequenceNumber compound), the single-field "eventStreamType" (covered by the eventStreamType_eventStreamId
    /// compound), and the "contentHashes" wildcard index (never filtered on).
    /// </summary>
    static readonly string[] _obsoleteEventIndexNames = ["eventSourceId", "eventTypeId", "eventStreamType", "contentHashes"];

    readonly IMongoDatabase _database;
    readonly ConcurrentDictionary<EventSequenceId, bool> _indexedEventSequences = [];
    readonly ConcurrentDictionary<string, byte> _indexedMutationHistoryCollections = [];

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
        var databaseName = DatabaseNames.ForEventStoreNamespace(eventStore, @namespace);
        var urlBuilder = new MongoUrlBuilder(mongoDBOptions.Value.Server)
        {
            DatabaseName = databaseName
        };
        if (mongoDBOptions.Value.DirectConnection == true)
        {
            urlBuilder.DirectConnection = true;
        }
        var settings = MongoClientSettings.FromUrl(urlBuilder.ToMongoUrl());

        // TODO: Performance optimization - separate reads from writes in a clustered setup. Read from secondary.
        // settings.ReadPreference = ReadPreference.SecondaryPreferred;
        Client = clientManager.GetClientFor(settings);
        _database = Client.GetDatabase(databaseName);
    }

    /// <inheritdoc/>
    public IMongoClient Client { get; }

    /// <inheritdoc/>
    public IMongoCollection<T> GetCollection<T>(string? name = null) => name == null ? _database.GetCollection<T>() : _database.GetCollection<T>(name);

    /// <inheritdoc/>
    public IMongoCollection<Event> GetEventSequenceCollectionFor(EventSequenceId eventSequenceId)
    {
        var collectionName = GetCollectionNameFor(eventSequenceId);
        return _database.GetCollection<Event>(collectionName);
    }

    /// <inheritdoc/>
    public IMongoCollection<BsonDocument> GetEventSequenceCollectionAsBsonFor(EventSequenceId eventSequenceId)
    {
        var collectionName = GetCollectionNameFor(eventSequenceId);
        return _database.GetCollection<BsonDocument>(collectionName);
    }

    /// <inheritdoc/>
    public IMongoCollection<ObserverState> GetObserverStateCollection() => GetCollection<ObserverState>(WellKnownCollectionNames.Observers);

    /// <inheritdoc/>
    public async Task EnsureIndexesForEventSequence(EventSequenceId eventSequenceId)
    {
        await EnsureMutationHistoryIndexes().ConfigureAwait(false);

        if (_indexedEventSequences.ContainsKey(eventSequenceId))
        {
            return;
        }

        var collection = _database.GetCollection<Event>(GetCollectionNameFor(eventSequenceId));
        var existing = await collection.GetIndexNamesAsync().ConfigureAwait(false);

        foreach (var obsolete in _obsoleteEventIndexNames.Where(existing.Contains))
        {
            await collection.Indexes.DropOneAsync(obsolete).ConfigureAwait(false);
        }

        var missing = DesiredEventIndexes().Where(model => !existing.Contains(model.Options.Name)).ToArray();
        if (missing.Length > 0)
        {
            await collection.Indexes.CreateManyAsync(missing).ConfigureAwait(false);
        }

        _indexedEventSequences.TryAdd(eventSequenceId, true);
    }

    static IEnumerable<CreateIndexModel<Event>> DesiredEventIndexes()
    {
        yield return new(
            Builders<Event>.IndexKeys.Ascending(_ => _.Type).Descending(_ => _.SequenceNumber),
            new CreateIndexOptions { Name = "type_sequenceNumber", Background = true });

        yield return new(
            Builders<Event>.IndexKeys.Ascending(_ => _.Occurred),
            new CreateIndexOptions { Name = "occurred", Background = true });

        yield return new(
            Builders<Event>.IndexKeys.Combine(
                Builders<Event>.IndexKeys.Ascending(_ => _.LastMutationOrdinal),
                Builders<Event>.IndexKeys.Ascending(_ => _.SequenceNumber)),
            new CreateIndexOptions { Name = "lastMutationOrdinal_sequenceNumber", Background = true, Sparse = false });

        yield return new(
            Builders<Event>.IndexKeys.Ascending(_ => _.EventSourceType),
            new CreateIndexOptions { Name = "eventSourceType", Background = true });

        yield return new(
            Builders<Event>.IndexKeys.Ascending(_ => _.EventStreamId),
            new CreateIndexOptions { Name = "eventStreamId", Background = true });

        yield return new(
            Builders<Event>.IndexKeys.Combine(
                Builders<Event>.IndexKeys.Ascending(_ => _.EventSourceId),
                Builders<Event>.IndexKeys.Ascending(_ => _.Type)),
            new CreateIndexOptions { Name = "eventSourceId_eventTypeId", Background = true });

        yield return new(
            Builders<Event>.IndexKeys.Combine(
                Builders<Event>.IndexKeys.Ascending(_ => _.EventStreamType),
                Builders<Event>.IndexKeys.Ascending(_ => _.EventStreamId)),
            new CreateIndexOptions { Name = "eventStreamType_eventStreamId", Background = true });

        yield return new(
            Builders<Event>.IndexKeys.Combine(
                Builders<Event>.IndexKeys.Ascending(_ => _.EventStreamType),
                Builders<Event>.IndexKeys.Ascending(_ => _.EventStreamId),
                Builders<Event>.IndexKeys.Ascending(_ => _.EventSourceType)),
            new CreateIndexOptions { Name = "eventStreamType_eventStreamId_eventSourceType", Background = true });

        yield return new(
            Builders<Event>.IndexKeys.Combine(
                Builders<Event>.IndexKeys.Ascending(_ => _.EventSourceId),
                Builders<Event>.IndexKeys.Ascending(_ => _.EventStreamType),
                Builders<Event>.IndexKeys.Ascending(_ => _.EventStreamId)),
            new CreateIndexOptions { Name = "eventSourceId_eventStreamType_eventStreamId", Background = true });

        yield return new(
            Builders<Event>.IndexKeys.Combine(
                Builders<Event>.IndexKeys.Ascending(_ => _.EventSourceId),
                Builders<Event>.IndexKeys.Ascending(_ => _.EventStreamType),
                Builders<Event>.IndexKeys.Ascending(_ => _.EventStreamId),
                Builders<Event>.IndexKeys.Ascending(_ => _.EventSourceType)),
            new CreateIndexOptions { Name = "eventSourceId_eventStreamType_eventStreamId_eventSourceType", Background = true });

        yield return new(
            Builders<Event>.IndexKeys.Combine(
                Builders<Event>.IndexKeys.Ascending(_ => _.EventSourceId),
                Builders<Event>.IndexKeys.Ascending(_ => _.Type),
                Builders<Event>.IndexKeys.Ascending(_ => _.EventStreamType),
                Builders<Event>.IndexKeys.Ascending(_ => _.EventStreamId)),
            new CreateIndexOptions { Name = "eventSourceId_eventTypeId_eventStreamType_eventStreamId", Background = true });

        yield return new(
            Builders<Event>.IndexKeys.Combine(
                Builders<Event>.IndexKeys.Ascending(_ => _.EventSourceId),
                Builders<Event>.IndexKeys.Ascending(_ => _.Type),
                Builders<Event>.IndexKeys.Ascending(_ => _.EventStreamType),
                Builders<Event>.IndexKeys.Ascending(_ => _.EventStreamId),
                Builders<Event>.IndexKeys.Ascending(_ => _.EventSourceType)),
            new CreateIndexOptions { Name = "eventSourceId_eventTypeId_eventStreamType_eventStreamId_eventSourceType", Background = true });

        yield return new(
            Builders<Event>.IndexKeys.Ascending(x => x.Tags),
            new CreateIndexOptions { Name = "tags" });
    }

    async Task EnsureMutationHistoryIndexes()
    {
        var collection = GetCollection<EventSequenceMutationHistoryEntry>(WellKnownCollectionNames.EventSequenceMutationHistory);
        await collection.EnsureIndexesOnceAsync(
            _indexedMutationHistoryCollections,
            new CreateIndexModel<EventSequenceMutationHistoryEntry>(
                Builders<EventSequenceMutationHistoryEntry>.IndexKeys.Combine(
                    Builders<EventSequenceMutationHistoryEntry>.IndexKeys.Ascending(_ => _.EventSequenceId),
                    Builders<EventSequenceMutationHistoryEntry>.IndexKeys.Ascending(_ => _.Ordinal)),
                new CreateIndexOptions { Name = "eventSequenceId_ordinal", Background = true, Sparse = false, Unique = true })).ConfigureAwait(false);
    }

    string GetCollectionNameFor(EventSequenceId eventSequenceId) => eventSequenceId.Value;
}
