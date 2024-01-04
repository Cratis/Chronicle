// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Text.Json;
using Aksio.Cratis.Events.MongoDB.EventTypes;
using Aksio.Cratis.Kernel.Compliance;
using Aksio.Cratis.Kernel.Storage;
using Aksio.Cratis.Kernel.Storage.EventTypes;
using Aksio.Cratis.Kernel.Storage.Identities;
using Aksio.Cratis.Kernel.Storage.MongoDB;
using Aksio.Cratis.Kernel.Storage.MongoDB.Identities;
using Aksio.Cratis.Kernel.Storage.MongoDB.Projections;
using Aksio.Cratis.Kernel.Storage.Projections;
using Aksio.Cratis.Projections.Json;
using Aksio.MongoDB;
using Microsoft.Extensions.Logging;

namespace Aksio.Cratis.MongoDB;

/// <summary>
/// Represents an implementation of <see cref="IEventStoreStorage"/> for MongoDB.
/// </summary>
public class EventStoreStorage : IEventStoreStorage
{
    readonly ConcurrentDictionary<TenantId, IEventStoreInstanceStorage> _instances = new();
    readonly IJsonComplianceManager _complianceManager;
    readonly Json.ExpandoObjectConverter _expandoObjectConverter;
    readonly JsonSerializerOptions _jsonSerializerOptions;
    readonly IMongoDBClientFactory _mongoDBClientFactory;
    readonly Kernel.Configuration.Storage _configuration;
    readonly IExecutionContextManager _executionContextManager;
    readonly ILoggerFactory _loggerFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventStoreStorage"/> class.
    /// </summary>
    /// <param name="eventStore"><see cref="EventStore"/> the storage is for.</param>
    /// <param name="clusterDatabase"><see cref="IClusterDatabase"/> to use.</param>
    /// <param name="eventStoreDatabase"><see cref="IEventStoreDatabase"/> to use.</param>
    /// <param name="projectionSerializer"><see cref="IJsonProjectionSerializer"/> for handling serialization of projection definitions.</param>
    /// <param name="projectionPipelineSerializer"><see cref="IJsonProjectionPipelineSerializer"/> for handling serialization of projection pipeline definitions.</param>
    /// <param name="complianceManager"><see cref="IJsonComplianceManager"/> for handling compliance.</param>
    /// <param name="expandoObjectConverter"><see cref="Json.ExpandoObjectConverter"/> for conversions.</param>
    /// <param name="jsonSerializerOptions">The global <see cref="JsonSerializerOptions"/>.</param>
    /// <param name="mongoDBClientFactory"><see cref="IMongoDBClientFactory"/> for creating clients.</param>
    /// <param name="configuration"><see cref="Kernel.Configuration.Storage"/> configuration.</param>
    /// <param name="executionContextManager"><see cref="IExecutionContextManager"/> for getting the execution context.</param>
    /// <param name="loggerFactory"><see cref="ILoggerFactory"/> for creating loggers.</param>
    public EventStoreStorage(
        EventStore eventStore,
        IClusterDatabase clusterDatabase,
        IEventStoreDatabase eventStoreDatabase,
        IJsonProjectionSerializer projectionSerializer,
        IJsonProjectionPipelineSerializer projectionPipelineSerializer,
        IJsonComplianceManager complianceManager,
        Json.ExpandoObjectConverter expandoObjectConverter,
        JsonSerializerOptions jsonSerializerOptions,
        IMongoDBClientFactory mongoDBClientFactory,
        Kernel.Configuration.Storage configuration,
        IExecutionContextManager executionContextManager,
        ILoggerFactory loggerFactory)
    {
        EventStore = eventStore;
        _complianceManager = complianceManager;
        _expandoObjectConverter = expandoObjectConverter;
        _jsonSerializerOptions = jsonSerializerOptions;
        _mongoDBClientFactory = mongoDBClientFactory;
        _configuration = configuration;
        _executionContextManager = executionContextManager;
        _loggerFactory = loggerFactory;
        Identities = new IdentityStorage(clusterDatabase, loggerFactory.CreateLogger<IdentityStorage>());
        EventTypes = new EventTypesStorage(eventStore, eventStoreDatabase, loggerFactory.CreateLogger<EventTypesStorage>());
        Projections = new ProjectionDefinitionsStorage(eventStoreDatabase, projectionSerializer);
        ProjectionPipelines = new ProjectionPipelineDefinitionsStorage(eventStoreDatabase, projectionPipelineSerializer);
    }

    /// <inheritdoc/>
    public EventStore EventStore { get; }

    /// <inheritdoc/>
    public IIdentityStorage Identities { get; }

    /// <inheritdoc/>
    public IEventTypesStorage EventTypes { get; }

    /// <inheritdoc/>
    public IProjectionDefinitionsStorage Projections { get; }

    /// <inheritdoc/>
    public IProjectionPipelineDefinitionsStorage ProjectionPipelines { get; }

    /// <inheritdoc/>
    public IEventStoreInstanceStorage GetInstance(TenantId tenantId)
    {
        if (_instances.TryGetValue(tenantId, out var instance))
        {
            return instance;
        }

        var database = new EventStoreInstanceDatabase(
            EventStore,
            tenantId,
            _mongoDBClientFactory,
            _configuration);

        var converter = new EventConverter(
            tenantId,
            EventTypes,
            Identities,
            _complianceManager,
            _expandoObjectConverter);

        return _instances[tenantId] =
            new EventStoreInstanceStorage(
                EventStore,
                tenantId,
                database,
                converter,
                EventTypes,
                _expandoObjectConverter,
                _jsonSerializerOptions,
                _executionContextManager,
                _loggerFactory);
    }
}
