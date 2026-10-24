// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using System.Text.Json;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Configuration;
using Cratis.Reactive;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB;

/// <summary>
/// Represents an implementation of <see cref="IClusterStorage"/> for MongoDB.
/// </summary>
/// <param name="database">The <see cref="IDatabase"/> instance.</param>
/// <param name="expandoObjectConverter">The <see cref="Json.IExpandoObjectConverter"/> instance.</param>
/// <param name="jsonSerializerOptions">The <see cref="JsonSerializerOptions"/> instance.</param>
/// <param name="jobsStorage">The <see cref="Cratis.Orleans.Storage.IJobsStorage"/> the job system resolves through.</param>
/// <param name="options">The <see cref="IOptions{ChronicleOptions}"/> instance.</param>
/// <param name="loggerFactory">The <see cref="ILoggerFactory"/> instance.</param>
public class ClusterStorage(
    IDatabase database,
    Json.IExpandoObjectConverter expandoObjectConverter,
    JsonSerializerOptions jsonSerializerOptions,
    Cratis.Orleans.Storage.IJobsStorage jobsStorage,
    IOptions<ChronicleOptions> options,
    ILoggerFactory loggerFactory) : IClusterStorage
{
    /// <inheritdoc/>
    public async Task<IReadOnlyDictionary<EventStoreName, Concepts.Projections.Generations.Enablement.GenerationFloor>> ReadGenerationFloors()
    {
        var floors = new Dictionary<EventStoreName, Concepts.Projections.Generations.Enablement.GenerationFloor>();
        foreach (var store in (await GetEventStores()).Concat(await DiscoverEventStores()).Append(EventStoreName.System)
            .Distinct(CaseInsensitiveEventStoreNameComparer.Instance).OrderBy(_ => _.Value, StringComparer.Ordinal))
        {
            var collection = database.GetEventStoreDatabase(store).GetCollection<BsonDocument>("generation-floor");
            if (await new Generations.GenerationFloorStorage(collection).Get() is { } floor)
            {
                floors.Add(store, floor);
            }
        }
        return floors;
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<EventStoreName>> GetEventStores()
    {
        var collection = GetCollection();
        using var result = await collection.FindAsync(_ => true);
        return (await result.ToListAsync()).Select(_ => _.Name);
    }

    /// <inheritdoc/>
    public ISubject<IEnumerable<EventStoreName>> ObserveEventStores()
    {
        var collection = GetCollection();
        return new TransformingSubject<IEnumerable<EventStore>, IEnumerable<EventStoreName>>(
            collection.Observe(),
            _ => _.Select(_ => _.Name));
    }

    /// <inheritdoc/>
    public IEventStoreStorage CreateStorageForEventStore(EventStoreName eventStore, SinksFactory sinksFactory) => new EventStoreStorage(
        eventStore,
        database.GetEventStoreDatabase(eventStore),
        expandoObjectConverter,
        jsonSerializerOptions,
        sinksFactory,
        jobsStorage,
        options,
        loggerFactory);

    /// <inheritdoc/>
    public async Task SaveEventStore(EventStoreName eventStore)
    {
        var collection = GetCollection();
        await DuplicateKeyConvergence.UpsertWithRetry(() => collection.ReplaceOneAsync(_ => _.Name == eventStore, new EventStore(eventStore), new ReplaceOptions { IsUpsert = true }));
    }

    async Task<IEnumerable<EventStoreName>> DiscoverEventStores()
    {
        var cluster = GetCollection().Database;

        // The cluster name is always the configured prefix followed by the well-known cluster name.
        var prefix = cluster.DatabaseNamespace.DatabaseName[..^WellKnownDatabaseNames.Chronicle.Length];
        using var cursor = await cluster.Client.ListDatabaseNamesAsync();
        return (await cursor.ToListAsync())
            .Where(_ => _.StartsWith(prefix, StringComparison.Ordinal) && _.EndsWith("+es", StringComparison.Ordinal))
            .Select(_ => _[prefix.Length..^3])
            .Where(_ => !string.IsNullOrWhiteSpace(_) && !_.Contains('+'))
            .Select(_ => new EventStoreName(_));
    }

    IMongoCollection<EventStore> GetCollection() => database.GetCollection<EventStore>(WellKnownCollectionNames.EventStores);
}
