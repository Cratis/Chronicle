// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Aksio.Cratis.Kernel.Compliance;
using Aksio.Cratis.Kernel.Configuration;
using Aksio.Cratis.Kernel.Storage;
using Aksio.Cratis.Projections.Json;
using Aksio.MongoDB;
using Microsoft.Extensions.Logging;

namespace Aksio.Cratis.MongoDB;

/// <summary>
/// Represents an implementation of <see cref="IClusterDatabase"/> for MongoDB.
/// </summary>
public class ClusterStorage : IClusterStorage
{
    readonly IDictionary<EventStore, IEventStoreStorage> _eventStores = new Dictionary<EventStore, IEventStoreStorage>();
    readonly IClusterDatabase _database;
    readonly IJsonProjectionSerializer _projectionSerializer;
    readonly IJsonProjectionPipelineSerializer _projectionPipelineSerializer;
    readonly IJsonComplianceManager _complianceManager;
    readonly Json.ExpandoObjectConverter _expandoObjectConverter;
    readonly JsonSerializerOptions _jsonSerializerOptions;
    readonly IMongoDBClientFactory _clientFactory;
    readonly Storage _configuration;
    readonly IExecutionContextManager _executionContextManager;
    readonly ILoggerFactory _loggerFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="ClusterStorage"/> class.
    /// </summary>
    /// <param name="projectionSerializer"><see cref="IJsonProjectionSerializer"/> for handling serialization of projection definitions.</param>
    /// <param name="projectionPipelineSerializer"><see cref="IJsonProjectionPipelineSerializer"/> for handling serialization of projection pipeline definitions.</param>
    /// <param name="complianceManager"><see cref="IJsonComplianceManager"/> for handling compliance.</param>
    /// <param name="expandoObjectConverter"><see cref="Json.ExpandoObjectConverter"/> for conversions.</param>
    /// <param name="jsonSerializerOptions">The global <see cref="JsonSerializerOptions"/>.</param>
    /// <param name="clientFactory"><see cref="IMongoDBClientFactory"/> for creating MongoDB clients.</param>
    /// <param name="configuration"><see cref="Storage"/> configuration.</param>
    /// <param name="executionContextManager"><see cref="IExecutionContextManager"/> for getting the execution context.</param>
    /// <param name="loggerFactory"><see cref="ILoggerFactory"/> for creating loggers.</param>
    public ClusterStorage(
        IJsonProjectionSerializer projectionSerializer,
        IJsonProjectionPipelineSerializer projectionPipelineSerializer,
        IJsonComplianceManager complianceManager,
        Json.ExpandoObjectConverter expandoObjectConverter,
        JsonSerializerOptions jsonSerializerOptions,
        IMongoDBClientFactory clientFactory,
        Storage configuration,
        IExecutionContextManager executionContextManager,
        ILoggerFactory loggerFactory)
    {
        _database = new ClusterDatabase(clientFactory, configuration);
        _projectionSerializer = projectionSerializer;
        _projectionPipelineSerializer = projectionPipelineSerializer;
        _complianceManager = complianceManager;
        _expandoObjectConverter = expandoObjectConverter;
        _jsonSerializerOptions = jsonSerializerOptions;
        _clientFactory = clientFactory;
        _configuration = configuration;
        _executionContextManager = executionContextManager;
        _loggerFactory = loggerFactory;
    }

    /// <inheritdoc/>
    public IEventStoreStorage GetEventStore(EventStore eventStore)
    {
        if (_eventStores.TryGetValue(eventStore, out var storage))
        {
            return storage;
        }

        return _eventStores[eventStore] = new EventStoreStorage(
            eventStore,
            _database,
            new EventStoreDatabase(eventStore, _clientFactory, _configuration),
            _projectionSerializer,
            _projectionPipelineSerializer,
            _complianceManager,
            _expandoObjectConverter,
            _jsonSerializerOptions,
            _clientFactory,
            _configuration,
            _executionContextManager,
            _loggerFactory);
    }
}
