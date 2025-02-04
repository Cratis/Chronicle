// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Reactive.Subjects;
using System.Text.Json;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Jobs;
using Cratis.Chronicle.Concepts.Observation.Reactors.Json;
using Cratis.Chronicle.Concepts.Observation.Reducers.Json;
using Cratis.Chronicle.Concepts.Projections.Json;
using Cratis.Chronicle.Reactive;
using Cratis.Chronicle.Storage.Sinks;
using Cratis.Types;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB;

/// <summary>
/// Represents an implementation of <see cref="IStorage"/> for MongoDB.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="Storage"/> class.
/// </remarks>
/// <param name="database">The MongoDB <see cref="IDatabase"/>.</param>
/// <param name="projectionSerializer"><see cref="IJsonProjectionDefinitionSerializer"/> for handling serialization of projection definitions.</param>
/// <param name="reactorSerializer"><see cref="IJsonReactorDefinitionSerializer"/> for handling serialization of reactor definitions.</param>
/// <param name="reducerSerializer"><see cref="IJsonReducerDefinitionSerializer"/> for handling serialization of reducer definitions.</param>
/// <param name="complianceManager"><see cref="IJsonComplianceManager"/> for handling compliance.</param>
/// <param name="expandoObjectConverter"><see cref="Json.IExpandoObjectConverter"/> for conversions.</param>
/// <param name="jsonSerializerOptions">The global <see cref="JsonSerializerOptions"/>.</param>
/// <param name="sinkFactories"><see cref="IInstancesOf{T}"/> for getting all <see cref="ISinkFactory"/> instances.</param>
/// <param name="jobTypes"><see cref="IJobTypes"/>.</param>
/// <param name="loggerFactory"><see cref="ILoggerFactory"/> for creating loggers.</param>
public class Storage(
    IDatabase database,
    IJsonProjectionDefinitionSerializer projectionSerializer,
    IJsonReactorDefinitionSerializer reactorSerializer,
    IJsonReducerDefinitionSerializer reducerSerializer,
    IJsonComplianceManager complianceManager,
    Json.IExpandoObjectConverter expandoObjectConverter,
    JsonSerializerOptions jsonSerializerOptions,
    IInstancesOf<ISinkFactory> sinkFactories,
    IJobTypes jobTypes,
    ILoggerFactory loggerFactory) : IStorage
{
    readonly ConcurrentDictionary<EventStoreName, IEventStoreStorage> _eventStores = [];

    /// <inheritdoc/>
    public async Task<IEnumerable<EventStoreName>> GetEventStores()
    {
        var collection = GetCollection();
        var result = await collection.FindAsync(_ => true);
        return result.ToList().Select(_ => _.Name);
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
    public IEventStoreStorage GetEventStore(EventStoreName eventStore)
    {
        if (_eventStores.TryGetValue(eventStore, out var storage))
        {
            return storage;
        }

        var collection = GetCollection();
        collection.ReplaceOne(_ => _.Name == eventStore, new EventStore(eventStore), new ReplaceOptions { IsUpsert = true });

        var eventStoreStorage = new EventStoreStorage(
            eventStore,
            database.GetEventStoreDatabase(eventStore),
            projectionSerializer,
            reactorSerializer,
            reducerSerializer,
            complianceManager,
            expandoObjectConverter,
            jsonSerializerOptions,
            sinkFactories,
            jobTypes,
            loggerFactory);

        return _eventStores[eventStore] = eventStoreStorage;
    }

    IMongoCollection<EventStore> GetCollection() => database.GetCollection<EventStore>(WellKnownCollectionNames.EventStores);
}
