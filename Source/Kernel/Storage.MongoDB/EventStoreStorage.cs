// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Text.Json;
using Cratis.Events.MongoDB.EventTypes;
using Cratis.Kernel.Compliance;
using Cratis.Kernel.Storage.EventTypes;
using Cratis.Kernel.Storage.Identities;
using Cratis.Kernel.Storage.MongoDB.Identities;
using Cratis.Kernel.Storage.MongoDB.Projections;
using Cratis.Kernel.Storage.Projections;
using Cratis.Projections.Json;
using Microsoft.Extensions.Logging;

namespace Cratis.Kernel.Storage.MongoDB;

/// <summary>
/// Represents an implementation of <see cref="IEventStoreStorage"/> for MongoDB.
/// </summary>
public class EventStoreStorage : IEventStoreStorage
{
    readonly ConcurrentDictionary<EventStoreNamespaceName, IEventStoreNamespaceStorage> _namespaces = new();
    readonly IEventStoreDatabase _eventStoreDatabase;
    readonly IJsonComplianceManager _complianceManager;
    readonly Json.ExpandoObjectConverter _expandoObjectConverter;
    readonly JsonSerializerOptions _jsonSerializerOptions;
    readonly IExecutionContextManager _executionContextManager;
    readonly ILoggerFactory _loggerFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventStoreStorage"/> class.
    /// </summary>
    /// <param name="eventStore"><see cref="EventStore"/> the storage is for.</param>
    /// <param name="database"><see cref="IDatabase"/> to use.</param>
    /// <param name="eventStoreDatabase"><see cref="IEventStoreDatabase"/> to use.</param>
    /// <param name="projectionSerializer"><see cref="IJsonProjectionSerializer"/> for handling serialization of projection definitions.</param>
    /// <param name="projectionPipelineSerializer"><see cref="IJsonProjectionPipelineSerializer"/> for handling serialization of projection pipeline definitions.</param>
    /// <param name="complianceManager"><see cref="IJsonComplianceManager"/> for handling compliance.</param>
    /// <param name="expandoObjectConverter"><see cref="Json.ExpandoObjectConverter"/> for conversions.</param>
    /// <param name="jsonSerializerOptions">The global <see cref="JsonSerializerOptions"/>.</param>
    /// <param name="executionContextManager"><see cref="IExecutionContextManager"/> for getting the execution context.</param>
    /// <param name="loggerFactory"><see cref="ILoggerFactory"/> for creating loggers.</param>
    public EventStoreStorage(
        EventStoreName eventStore,
        IDatabase database,
        IEventStoreDatabase eventStoreDatabase,
        IJsonProjectionSerializer projectionSerializer,
        IJsonProjectionPipelineSerializer projectionPipelineSerializer,
        IJsonComplianceManager complianceManager,
        Json.ExpandoObjectConverter expandoObjectConverter,
        JsonSerializerOptions jsonSerializerOptions,
        IExecutionContextManager executionContextManager,
        ILoggerFactory loggerFactory)
    {
        EventStore = eventStore;
        _eventStoreDatabase = eventStoreDatabase;
        _complianceManager = complianceManager;
        _expandoObjectConverter = expandoObjectConverter;
        _jsonSerializerOptions = jsonSerializerOptions;
        _executionContextManager = executionContextManager;
        _loggerFactory = loggerFactory;
        Identities = new IdentityStorage(database, loggerFactory.CreateLogger<IdentityStorage>());
        EventTypes = new EventTypesStorage(eventStore, eventStoreDatabase, loggerFactory.CreateLogger<EventTypesStorage>());
        Projections = new ProjectionDefinitionsStorage(eventStoreDatabase, projectionSerializer);
        ProjectionPipelines = new ProjectionPipelineDefinitionsStorage(eventStoreDatabase, projectionPipelineSerializer);
    }

    /// <inheritdoc/>
    public EventStoreName EventStore { get; }

    /// <inheritdoc/>
    public IIdentityStorage Identities { get; }

    /// <inheritdoc/>
    public IEventTypesStorage EventTypes { get; }

    /// <inheritdoc/>
    public IProjectionDefinitionsStorage Projections { get; }

    /// <inheritdoc/>
    public IProjectionPipelineDefinitionsStorage ProjectionPipelines { get; }

    /// <inheritdoc/>
    public IEventStoreNamespaceStorage GetNamespace(EventStoreNamespaceName @namespace)
    {
        if (_namespaces.TryGetValue(@namespace, out var instance))
        {
            return instance;
        }

        var converter = new EventConverter(
            EventStore,
            @namespace,
            EventTypes,
            Identities,
            _complianceManager,
            _expandoObjectConverter);

        return _namespaces[@namespace] =
            new EventStoreNamespaceStorage(
                EventStore,
                @namespace,
                _eventStoreDatabase.GetNamespaceDatabase(@namespace),
                converter,
                EventTypes,
                _expandoObjectConverter,
                _jsonSerializerOptions,
                _executionContextManager,
                _loggerFactory);
    }
}
