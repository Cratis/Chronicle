// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Reactive.Subjects;
using System.Text.Json;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.EventStores;
using Cratis.Chronicle.Storage.EventSequences.Mutations;
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
/// <param name="jobsStorage">The <see cref="Cratis.Choreography.Storage.IJobsStorage"/> the job system resolves through.</param>
/// <param name="options">The <see cref="IOptions{ChronicleOptions}"/> instance.</param>
/// <param name="loggerFactory">The <see cref="ILoggerFactory"/> instance.</param>
/// <param name="mutationDigestKeys">The source of mutation digest keys; without one, every mutation fails closed.</param>
public class ClusterStorage(
    IDatabase database,
    Json.IExpandoObjectConverter expandoObjectConverter,
    JsonSerializerOptions jsonSerializerOptions,
    Cratis.Choreography.Storage.IJobsStorage jobsStorage,
    IOptions<ChronicleOptions> options,
    ILoggerFactory loggerFactory,
    IEventSequenceMutationDigestKeys? mutationDigestKeys = null) : IClusterStorage
{
    readonly ConcurrentDictionary<string, bool> _skippedNames = new();

    /// <inheritdoc/>
    public async Task<IReadOnlyDictionary<EventStoreName, Concepts.Projections.Generations.Enablement.GenerationFloor>> ReadGenerationFloors()
    {
        var floors = new Dictionary<EventStoreName, Concepts.Projections.Generations.Enablement.GenerationFloor>();
        foreach (var store in (await GetEventStores()).Concat(await DiscoverEventStores()).Append(EventStoreName.System)
            .Distinct(CaseInsensitiveEventStoreNameComparer.Instance).OrderBy(_ => _.Value, StringComparer.Ordinal))
        {
            var collection = database.GetEventStoreDatabase(store).GetCollection<BsonDocument>(WellKnownCollectionNames.GenerationFloor);
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
        return Usable((await result.ToListAsync()).Select(_ => _.Name));
    }

    /// <inheritdoc/>
    public ISubject<IEnumerable<EventStoreName>> ObserveEventStores()
    {
        var collection = GetCollection();
        return new TransformingSubject<IEnumerable<EventStore>, IEnumerable<EventStoreName>>(
            collection.Observe(),
            _ => Usable(_.Select(_ => _.Name)));
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
        loggerFactory,
        mutationDigestKeys);

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
            .Where(_ => !string.IsNullOrWhiteSpace(_) && !_.Contains('+') && !ReservedEventStoreNames.IsInternalDatabaseName(_))
            .Select(_ => DatabaseNames.EventStoreFor(_, options.Value.Storage.SystemDatabaseName));
    }

    /// <summary>
    /// Leave out registered names that can no longer be resolved to a database, so one bad registration never stops a loop over the event stores.
    /// </summary>
    /// <param name="names">The registered names.</param>
    /// <returns>The names that can be resolved.</returns>
    List<EventStoreName> Usable(IEnumerable<EventStoreName> names)
    {
        var systemDatabaseName = options.Value.Storage.SystemDatabaseName;
        var usable = new List<EventStoreName>();
        foreach (var name in names)
        {
            if (name is null || name == EventStoreName.System || !ReservedEventStoreNames.IsReserved(name.Value, systemDatabaseName))
            {
                usable.Add(name!);
                continue;
            }

            if (_skippedNames.TryAdd(name.Value, true))
            {
                loggerFactory.CreateLogger<ClusterStorage>().SkippingEventStoreWithReservedName(name.Value);
            }
        }

        return usable;
    }

    IMongoCollection<EventStore> GetCollection() => database.GetCollection<EventStore>(WellKnownCollectionNames.EventStores);
}
