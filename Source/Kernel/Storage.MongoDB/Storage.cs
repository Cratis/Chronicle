// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Reactive.Subjects;
using System.Text.Json;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Jobs;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Storage.Sinks;
using Cratis.Reactive;
using Cratis.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB;

/// <summary>
/// Represents an implementation of <see cref="IStorage"/> for MongoDB.
/// </summary>
/// <param name="database">The MongoDB <see cref="IDatabase"/>.</param>
/// <param name="complianceManager"><see cref="IJsonComplianceManager"/> for handling compliance.</param>
/// <param name="expandoObjectConverter"><see cref="Json.IExpandoObjectConverter"/> for conversions.</param>
/// <param name="jsonSerializerOptions">The global <see cref="JsonSerializerOptions"/>.</param>
/// <param name="sinkFactories"><see cref="IInstancesOf{T}"/> for getting all <see cref="ISinkFactory"/> instances.</param>
/// <param name="jobTypes"><see cref="IJobTypes"/>.</param>
/// <param name="options"><see cref="ChronicleOptions"/>.</param>
/// <param name="loggerFactory"><see cref="ILoggerFactory"/> for creating loggers.</param>
public class Storage(
    IDatabase database,
    IJsonComplianceManager complianceManager,
    Json.IExpandoObjectConverter expandoObjectConverter,
    JsonSerializerOptions jsonSerializerOptions,
    IInstancesOf<ISinkFactory> sinkFactories,
    IJobTypes jobTypes,
    IOptions<ChronicleOptions> options,
    ILoggerFactory loggerFactory) : IStorage
{
    readonly ConcurrentDictionary<EventStoreName, IEventStoreStorage> _eventStores = [];

    /// <inheritdoc/>
    public ISystemStorage System => new SystemStorage(database);

    /// <inheritdoc/>
    public async Task<IEnumerable<EventStoreName>> GetEventStores()
    {
        var collection = GetCollection();
        using var result = await collection.FindAsync(_ => true);
        return result.ToList().Select(_ => _.Name);
    }

    /// <inheritdoc/>
    public async Task<bool> HasEventStore(EventStoreName eventStore)
    {
        var collection = GetCollection();
        var count = await collection.CountDocumentsAsync(_ => _.Name == eventStore);
        return count > 0;
    }

    /// <inheritdoc/>
    public ISubject<IEnumerable<EventStoreName>> ObserveEventStores()
    {
        var collection = GetCollection();
        return new TransformingSubject<IEnumerable<EventStore>, IEnumerable<EventStoreName>>(
            collection.Observe(),
            _ => _.Select(es => es.Name)
                  .Where(name => !string.IsNullOrWhiteSpace(name.Value) && name != EventStoreName.NotSet));
    }

    /// <inheritdoc/>
    public IEventStoreStorage GetEventStore(EventStoreName eventStore)
    {
        ThrowIfEventStoreNameIsInvalid(eventStore);

        var pair = _eventStores
            .Select(kvp => new { kvp.Key, kvp.Value })
            .FirstOrDefault(_ => _.Key.Value.Equals(eventStore.Value, StringComparison.InvariantCultureIgnoreCase));

        if (pair is not null)
        {
            return pair.Value;
        }

        var collection = GetCollection();
        collection.ReplaceOne(_ => _.Name == eventStore, new EventStore(eventStore), new ReplaceOptions { IsUpsert = true });

        var eventStoreStorage = new EventStoreStorage(
            eventStore,
            database.GetEventStoreDatabase(eventStore),
            complianceManager,
            expandoObjectConverter,
            jsonSerializerOptions,
            sinkFactories,
            jobTypes,
            options,
            loggerFactory);

        return _eventStores[eventStore] = eventStoreStorage;
    }

    void ThrowIfEventStoreNameIsInvalid(EventStoreName eventStore)
    {
        if (eventStore is null || string.IsNullOrWhiteSpace(eventStore.Value))
        {
            throw new InvalidEventStoreName(eventStore!, "EventStoreName cannot be null, empty or whitespace.");
        }

        if (eventStore == EventStoreName.NotSet)
        {
            throw new InvalidEventStoreName(eventStore, "EventStoreName cannot be '[NotSet]'. It must be properly configured.");
        }
    }

    IMongoCollection<EventStore> GetCollection() => database.GetCollection<EventStore>(WellKnownCollectionNames.EventStores);
}
