// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Text.Json;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Jobs;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Storage.Events.Constraints;
using Cratis.Chronicle.Storage.EventTypes;
using Cratis.Chronicle.Storage.MongoDB.Events.Constraints;
using Cratis.Chronicle.Storage.MongoDB.Events.EventTypes;
using Cratis.Chronicle.Storage.MongoDB.Namespaces;
using Cratis.Chronicle.Storage.MongoDB.Observation.Reactors;
using Cratis.Chronicle.Storage.MongoDB.Observation.Reducers;
using Cratis.Chronicle.Storage.MongoDB.Projections;
using Cratis.Chronicle.Storage.Namespaces;
using Cratis.Chronicle.Storage.Observation.Reactors;
using Cratis.Chronicle.Storage.Observation.Reducers;
using Cratis.Chronicle.Storage.Projections;
using Cratis.Chronicle.Storage.ReadModels;
using Cratis.Chronicle.Storage.Sinks;
using Cratis.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Storage.MongoDB;

/// <summary>
/// Represents an implementation of <see cref="IEventStoreStorage"/> for MongoDB.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="EventStoreStorage"/> class.
/// </remarks>
/// <param name="eventStore"><see cref="EventStore"/> the storage is for.</param>
/// <param name="eventStoreDatabase"><see cref="IEventStoreDatabase"/> to use.</param>
/// <param name="complianceManager"><see cref="IJsonComplianceManager"/> for handling compliance.</param>
/// <param name="expandoObjectConverter"><see cref="Json.IExpandoObjectConverter"/> for conversions.</param>
/// <param name="jsonSerializerOptions">The global <see cref="JsonSerializerOptions"/>.</param>
/// <param name="sinkFactories"><see cref="IInstancesOf{T}"/> for getting all <see cref="ISinkFactory"/> instances.</param>
/// <param name="jobTypes"><see cref="IJobTypes"/>.</param>
/// <param name="options"><see cref="ChronicleOptions"/>.</param>
/// <param name="loggerFactory"><see cref="ILoggerFactory"/> for creating loggers.</param>
public class EventStoreStorage(
    EventStoreName eventStore,
    IEventStoreDatabase eventStoreDatabase,
    IJsonComplianceManager complianceManager,
    Json.IExpandoObjectConverter expandoObjectConverter,
    JsonSerializerOptions jsonSerializerOptions,
    IInstancesOf<ISinkFactory> sinkFactories,
    IJobTypes jobTypes,
    IOptions<ChronicleOptions> options,
    ILoggerFactory loggerFactory) : IEventStoreStorage
{
    readonly ConcurrentDictionary<EventStoreNamespaceName, IEventStoreNamespaceStorage> _namespaces = new();

    /// <inheritdoc/>
    public EventStoreName EventStore { get; } = eventStore;

    /// <inheritdoc/>
    public INamespaceStorage Namespaces { get; } = new NamespaceStorage(eventStoreDatabase, loggerFactory.CreateLogger<NamespaceStorage>());

    /// <inheritdoc/>
    public IEventTypesStorage EventTypes { get; } = new EventTypesStorage(eventStore, eventStoreDatabase, loggerFactory.CreateLogger<EventTypesStorage>());

    /// <inheritdoc/>
    public IReactorDefinitionsStorage Reactors { get; } = new ReactorDefinitionsStorage(eventStoreDatabase);

    /// <inheritdoc/>
    public IReducerDefinitionsStorage Reducers { get; } = new ReducerDefinitionsStorage(eventStoreDatabase);

    /// <inheritdoc/>
    public IProjectionDefinitionsStorage Projections { get; } = new ProjectionDefinitionsStorage(eventStoreDatabase);

    /// <inheritdoc/>
    public IConstraintsStorage Constraints { get; } = new ConstraintsStorage(eventStoreDatabase);

    /// <inheritdoc/>
    public IReadModelDefinitionsStorage ReadModels { get; } = new ReadModelDefinitionsStorage(eventStoreDatabase);

    /// <inheritdoc/>
    public IEventStoreNamespaceStorage GetNamespace(EventStoreNamespaceName @namespace)
    {
        if (_namespaces.TryGetValue(@namespace, out var instance))
        {
            return instance;
        }

        return _namespaces[@namespace] =
            new EventStoreNamespaceStorage(
                EventStore,
                @namespace,
                eventStoreDatabase.GetNamespaceDatabase(@namespace),
                EventTypes,
                complianceManager,
                expandoObjectConverter,
                jsonSerializerOptions,
                sinkFactories,
                jobTypes,
                options,
                loggerFactory);
    }
}
